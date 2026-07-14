namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.DTOs.Decision;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Deterministic Diversion / Dynamic-Pricing agent — the "diversion flywheel".
/// It turns the 10-day holding window from a countdown-to-return into a
/// countdown-to-sale. All pricing and radius math is deterministic (never the
/// LLM) so the decisions are auditable.
///
/// The markdown is NOT driven by the calendar alone. Each price cut is a
/// function of three independent risk signals plus category economics:
///   • time pressure   — how far into the 10-day clock we are
///   • demand risk      — weak local demand (low match score) sells slower
///   • condition risk   — lower-grade stock resells slower
///   • category elasticity — how fast the category depreciates (caps the cut)
///   • value-at-risk    — premium items are discounted more gently
///
/// Escalation ladder as the clock advances without a strong local match:
///   day 0-3  strong match  -> SELL_LOCAL at base price
///   day 4-5                -> HOLD / WIDEN_RADIUS
///   day 6-7                -> DISCOUNT_SELL (price cut, wider radius)
///   day 8-9                -> OFFER_ACCESS_POINTS (deeper cut) or ESCALATE if weak demand
///   day 10                 -> RETURN_TO_SELLER (clock expired)
/// </summary>
public class DiversionAgentService : IDiversionAgentService
{
    private const double BaseRadiusKm = 10.0;
    private const int StrongMatch = 70;
    private const int WeakMatch = 30;
    private readonly ILogger<DiversionAgentService> _logger;

    // Category resale elasticity — the maximum markdown ceiling (%). Fast-
    // depreciating categories tolerate deeper cuts; value-stable categories cap
    // low so we protect margin and lean on radius/access points instead.
    private static readonly Dictionary<string, double> CategoryMaxDiscountMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Beauty"] = 45,
        ["Apparel"] = 42,
        ["Footwear"] = 40,
        ["Accessories"] = 38,
        ["Toys"] = 36,
        ["Electronics"] = 35,
        ["Sports"] = 34,
        ["Home"] = 26,
        ["Books"] = 20,
    };

    public DiversionAgentService(ILogger<DiversionAgentService> logger)
    {
        _logger = logger;
    }

    public DiversionDecision Decide(
        int matchScore,
        HoldingClockResult clock,
        decimal basePrice,
        bool resaleAllowed,
        string? condition = null,
        string? category = null)
    {
        var decision = new DiversionDecision
        {
            BasePrice = basePrice,
            SuggestedPrice = basePrice,
            SearchRadiusKm = BaseRadiusKm
        };

        // Compliance gate short-circuits diversion entirely.
        if (!resaleAllowed)
        {
            decision.Action = "RETURN_TO_SELLER";
            decision.Reasoning = "Retailer policy prohibits resale — routed back to seller.";
            return Log(decision, matchScore, clock);
        }

        // Clock expired: deterministic auto-return.
        if (clock.IsExpired)
        {
            decision.Action = "RETURN_TO_SELLER";
            decision.SuggestedPrice = basePrice;
            decision.Reasoning = $"Holding window elapsed (day {clock.HoldingDay}/{HoldingClockResult.MaxHoldingDays}); auto-returned to seller.";
            return Log(decision, matchScore, clock);
        }

        // --- Risk signals (each normalised 0..1) ---------------------------
        double timePressure = Math.Clamp(clock.HoldingDay / (double)HoldingClockResult.MaxHoldingDays, 0, 1);
        double demandRisk = Math.Clamp((StrongMatch - matchScore) / (double)StrongMatch, 0, 1);
        double conditionFactor = MatchCalculator.ConditionResaleScore(condition, 100) / 100.0; // 1=mint .. 0.2=damaged
        double conditionRisk = 1.0 - conditionFactor;

        // Probability the item clears locally within the window (demand + condition).
        double sellProbability = Math.Clamp(0.60 * (matchScore / 100.0) + 0.40 * conditionFactor, 0, 1);
        decision.SellProbability = Math.Round(sellProbability, 2);

        // Blended dead-stock risk (0 = safe, 1 = high risk) — drives markdown + radius.
        double clearanceRisk = Math.Clamp(0.45 * timePressure + 0.35 * demandRisk + 0.20 * conditionRisk, 0, 1);
        decision.ClearanceRisk = Math.Round(clearanceRisk, 2);

        // Strong local match early in the window: sell now at full price.
        if (matchScore >= StrongMatch && clock.HoldingDay <= 5)
        {
            decision.Action = "SELL_LOCAL";
            decision.Reasoning =
                $"Strong local demand (match {matchScore}, ~{sellProbability:P0} sell-through) with " +
                $"{clock.DaysRemaining} day(s) left — list locally at base price.";
            return Log(decision, matchScore, clock);
        }

        // --- Risk-weighted markdown ----------------------------------------
        // Depth = category ceiling × blended clearance risk × value guard.
        double maxDiscount = CategoryMaxDiscountPct(category);
        double valueGuard = ValueAtRiskGuard(basePrice);
        double discountPct = Math.Clamp(Math.Round(maxDiscount * clearanceRisk * valueGuard), 0, maxDiscount);

        // Radius widens with clearance risk: safe items stay hyperlocal, risky
        // items reach out to 2.5x the base radius to find a buyer.
        double radiusMultiplier = 1.0 + 1.5 * clearanceRisk;

        decision.PriceAdjustmentPct = -discountPct;
        decision.SuggestedPrice = Math.Round(basePrice * (decimal)(1 - discountPct / 100.0), 2);
        decision.SearchRadiusKm = Math.Round(BaseRadiusKm * radiusMultiplier, 1);

        // Weak demand near end of window with nothing landing -> escalate to a human.
        if (matchScore < WeakMatch && clock.HoldingDay >= 8)
        {
            decision.Action = "ESCALATE";
            decision.Escalated = true;
            decision.Reasoning =
                $"Weak local demand (match {matchScore}, ~{sellProbability:P0} sell-through) at day " +
                $"{clock.HoldingDay} — dead-stock risk; escalate for hold/return decision.";
            return Log(decision, matchScore, clock);
        }

        decision.Action = clock.HoldingDay switch
        {
            <= 5 => "WIDEN_RADIUS",
            <= 7 => "DISCOUNT_SELL",
            _    => "OFFER_ACCESS_POINTS"
        };

        decision.Reasoning =
            $"Day {clock.HoldingDay}/{HoldingClockResult.MaxHoldingDays} · match {matchScore} · " +
            $"~{sellProbability:P0} sell-through · clearance-risk {clearanceRisk:P0}: " +
            $"{decision.Action.Replace('_', ' ').ToLowerInvariant()} at {discountPct:0}% off " +
            $"(₹{decision.SuggestedPrice}) within {decision.SearchRadiusKm} km to divert before return.";

        return Log(decision, matchScore, clock);
    }

    /// <summary>Category depreciation ceiling — the deepest markdown allowed (%). Unknown categories cap at 32%.</summary>
    private static double CategoryMaxDiscountPct(string? category)
        => !string.IsNullOrWhiteSpace(category) && CategoryMaxDiscountMap.TryGetValue(category.Trim(), out var v)
            ? v
            : 32;

    /// <summary>Premium items lose more absolute margin per % cut, so discount them more gently and recover via reach.</summary>
    private static double ValueAtRiskGuard(decimal basePrice) => basePrice switch
    {
        <= 1000m => 1.0,
        <= 5000m => 0.9,
        _ => 0.8
    };

    private DiversionDecision Log(DiversionDecision d, int matchScore, HoldingClockResult clock)
    {
        _logger.LogInformation(
            "Diversion decision: {Action} (match {Match}, day {Day}, price {Price}, radius {Radius}km)",
            d.Action, matchScore, clock.HoldingDay, d.SuggestedPrice, d.SearchRadiusKm);
        return d;
    }
}
