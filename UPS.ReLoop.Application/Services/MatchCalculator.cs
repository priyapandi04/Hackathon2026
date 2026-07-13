namespace UPS.ReLoop.Application.Services;

using UPS.ReLoop.Application.DTOs.MatchAgent;
using UPS.ReLoop.Domain.Entities;

public static class MatchCalculator
{
    private const int SameProductPoints = 50;
    private const int SameAreaPoints = 20;
    private const int HighDemandPoints = 20;
    private const int GoodConditionPoints = 10;
    private const double HighDemandThreshold = 70.0; // demand score is on a 0-100 scale

    private static readonly HashSet<string> GoodConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "New", "Like New", "Good", "Excellent", "Refurbished"
    };

    // Category resale-velocity priors (0-100) for the hyperlocal demand model.
    // These encode how quickly each category clears on local secondary channels
    // (electronics/beauty move fastest, books slowest) and let the agent reason
    // about products it has never seen before, not only seeded SKUs.
    private static readonly Dictionary<string, int> CategoryDemandIndexMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Electronics"] = 90,
        ["Beauty"] = 84,
        ["Apparel"] = 82,
        ["Footwear"] = 80,
        ["Accessories"] = 76,
        ["Sports"] = 72,
        ["Toys"] = 70,
        ["Home"] = 66,
        ["Books"] = 46,
    };

    /// <summary>Category resale-velocity index (0-100). Unknown categories get a neutral 62.</summary>
    public static int CategoryDemandIndex(string? category)
        => !string.IsNullOrWhiteSpace(category) && CategoryDemandIndexMap.TryGetValue(category.Trim(), out var v)
            ? v
            : 62;

    /// <summary>Resale points for the reported condition, scaled to <paramref name="maxPoints"/>.</summary>
    public static int ConditionResaleScore(string? condition, int maxPoints)
    {
        var fraction = condition?.Trim().ToLowerInvariant() switch
        {
            "new" or "like new" or "excellent" or "refurbished" => 1.0,
            "good" => 0.75,
            "fair" => 0.45,
            "poor" or "damaged" => 0.2,
            _ => 0.6,
        };
        return (int)Math.Round(fraction * maxPoints);
    }

    public static (int Score, List<MatchDetail> Details) Calculate(
        MatchAgentRequest request,
        IReadOnlyList<InventoryPool> inventoryMatches,
        IReadOnlyList<DemandHistory> demandRecords)
    {
        var details = new List<MatchDetail>();
        int totalScore = 0;

        // Rule 1: Same Product match
        var productMatch = inventoryMatches.Any(i => i.ProductId == request.ProductId);
        if (productMatch)
        {
            totalScore += SameProductPoints;
            details.Add(new MatchDetail
            {
                Factor = "Same Product",
                Points = SameProductPoints,
                Reason = $"Product '{request.ProductId}' found in local inventory pool"
            });
        }

        // Rule 2: Same Area match
        var areaMatch = demandRecords.Any(d =>
            d.Region.Equals(request.Location, StringComparison.OrdinalIgnoreCase));
        if (areaMatch)
        {
            totalScore += SameAreaPoints;
            details.Add(new MatchDetail
            {
                Factor = "Same Area",
                Points = SameAreaPoints,
                Reason = $"Active demand detected in region '{request.Location}'"
            });
        }

        // Rule 3: High Demand Score
        var maxDemandScore = demandRecords
            .Where(d => d.ProductId == request.ProductId)
            .Select(d => d.DemandScore)
            .DefaultIfEmpty(0)
            .Max();

        if (maxDemandScore >= HighDemandThreshold)
        {
            totalScore += HighDemandPoints;
            details.Add(new MatchDetail
            {
                Factor = "High Demand",
                Points = HighDemandPoints,
                Reason = $"Demand score {maxDemandScore:F1} exceeds threshold of {HighDemandThreshold}"
            });
        }

        // Rule 4: Good Condition
        if (GoodConditions.Contains(request.Condition))
        {
            totalScore += GoodConditionPoints;
            details.Add(new MatchDetail
            {
                Factor = "Good Condition",
                Points = GoodConditionPoints,
                Reason = $"Product condition '{request.Condition}' qualifies for resale"
            });
        }

        return (Math.Min(totalScore, 100), details);
    }

    public static string DetermineRecommendation(int matchScore)
    {
        return matchScore switch
        {
            >= 80 => "SELL_LOCAL",
            >= 60 => "REDISTRIBUTE",
            >= 40 => "DISCOUNT_SELL",
            >= 20 => "WAREHOUSE_HOLD",
            _ => "LIQUIDATE"
        };
    }

    public static double CalculateConfidence(int matchScore, int detailCount)
    {
        // Confidence blends the match strength with how many factors backed it.
        // Three contributing signals (category, region, condition) is the healthy
        // baseline of the demand model, so the denominator is 3; exact-SKU hits
        // (local inventory / proven demand) push confidence higher still.
        // Returned on a 0-1 scale — the single convention shared by image,
        // match and decision confidence across the whole pipeline.
        var factorWeight = Math.Min(detailCount / 3.0, 1.4) * 100.0;
        var blended = Math.Min((matchScore + factorWeight) / 2.0, 99);
        return Math.Round(blended / 100.0, 2);
    }

    public static double EstimateDistanceSaved(int matchScore)
    {
        // Higher match = more local fulfillment = more distance saved
        return Math.Round(matchScore * 5.5, 0);
    }

    public static double EstimateCostSaved(double distanceSavedKm)
    {
        // Reverse road-freight avoided per parcel-km (INR), illustrative.
        return Math.Round(distanceSavedKm * 1.1, 2);
    }

    public static double EstimateCo2Saved(double distanceSavedKm)
    {
        // Approximate 0.0037 kg CO2 per km for ground shipping
        return Math.Round(distanceSavedKm * 0.0037, 2);
    }

    /// <summary>
    /// Deterministic expected days-to-sell — the playbook's Agent 1 output field.
    /// Stronger local demand clears faster; always inside the 10-day holding window.
    /// </summary>
    public static int EstimateDaysToSell(int matchScore)
    {
        var days = (int)Math.Round(10 - matchScore / 12.5);
        return Math.Clamp(days, 1, 10);
    }

    /// <summary>
    /// Resolves the local resale channel (the playbook's Agent 1 <c>channel</c> field).
    /// A staffed UPS Store is used when local demand is strong, otherwise a self-serve
    /// Access Point. The storefront id is a stable hash of the hub so the same location
    /// always maps to the same channel across runs.
    /// </summary>
    public static string ResolveChannel(string location, int matchScore)
    {
        var loc = string.IsNullOrWhiteSpace(location) ? "Hub" : location.Trim();
        int hash = 0;
        foreach (var c in loc) hash = (hash * 31 + c) & 0x7fffffff;
        var number = 100 + hash % 900;

        return matchScore >= 60
            ? $"UPS Store #{number} ({loc})"
            : $"UPS Access Point #{number} ({loc})";
    }
}
