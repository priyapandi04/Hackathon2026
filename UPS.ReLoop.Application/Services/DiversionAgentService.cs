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

    public DiversionAgentService(ILogger<DiversionAgentService> logger)
    {
        _logger = logger;
    }

    public DiversionDecision Decide(int matchScore, HoldingClockResult clock, decimal basePrice, bool resaleAllowed)
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

        // Strong local match early in the window: sell now at full price.
        if (matchScore >= StrongMatch && clock.HoldingDay <= 5)
        {
            decision.Action = "SELL_LOCAL";
            decision.Reasoning = $"Strong local demand (match {matchScore}) with {clock.DaysRemaining} day(s) left — list locally at base price.";
            return Log(decision, matchScore, clock);
        }

        // Progressive markdown + radius expansion as the clock advances.
        double discountPct = clock.HoldingDay switch
        {
            <= 3 => 0,
            <= 5 => 5,
            <= 7 => 12,
            8    => 20,
            _    => 30 // day 9
        };

        double radiusMultiplier = clock.HoldingDay switch
        {
            <= 5 => 1.0,
            <= 7 => 1.5,
            _    => 2.5
        };

        decision.PriceAdjustmentPct = -discountPct;
        decision.SuggestedPrice = Math.Round(basePrice * (decimal)(1 - discountPct / 100.0), 2);
        decision.SearchRadiusKm = Math.Round(BaseRadiusKm * radiusMultiplier, 1);

        // Weak demand near end of window with nothing landing -> escalate to a human.
        if (matchScore < WeakMatch && clock.HoldingDay >= 8)
        {
            decision.Action = "ESCALATE";
            decision.Escalated = true;
            decision.Reasoning = $"Weak local demand (match {matchScore}) at day {clock.HoldingDay} — dead-stock risk; escalate for hold/return decision.";
            return Log(decision, matchScore, clock);
        }

        decision.Action = clock.HoldingDay switch
        {
            <= 5 => "WIDEN_RADIUS",
            <= 7 => "DISCOUNT_SELL",
            _    => "OFFER_ACCESS_POINTS"
        };

        decision.Reasoning =
            $"Day {clock.HoldingDay}/{HoldingClockResult.MaxHoldingDays}, match {matchScore}: " +
            $"{decision.Action.Replace('_', ' ').ToLowerInvariant()} at {discountPct:0}% off " +
            $"(₹{decision.SuggestedPrice}) within {decision.SearchRadiusKm} km to divert before return.";

        return Log(decision, matchScore, clock);
    }

    private DiversionDecision Log(DiversionDecision d, int matchScore, HoldingClockResult clock)
    {
        _logger.LogInformation(
            "Diversion decision: {Action} (match {Match}, day {Day}, price {Price}, radius {Radius}km)",
            d.Action, matchScore, clock.HoldingDay, d.SuggestedPrice, d.SearchRadiusKm);
        return d;
    }
}
