namespace UPS.ReLoop.Application.Services;

using UPS.ReLoop.Application.DTOs.MatchAgent;
using UPS.ReLoop.Domain.Entities;

public static class MatchCalculator
{
    private const int SameProductPoints = 50;
    private const int SameAreaPoints = 20;
    private const int HighDemandPoints = 20;
    private const int GoodConditionPoints = 10;
    private const double HighDemandThreshold = 7.0;

    private static readonly HashSet<string> GoodConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "New", "Like New", "Good", "Excellent", "Refurbished"
    };

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
        // Confidence based on how many factors contributed
        var factorWeight = detailCount / 4.0 * 100.0;
        return Math.Min(Math.Round((matchScore + factorWeight) / 2.0, 0), 99);
    }

    public static double EstimateDistanceSaved(int matchScore)
    {
        // Higher match = more local fulfillment = more distance saved
        return Math.Round(matchScore * 5.5, 0);
    }

    public static double EstimateCostSaved(double distanceSavedKm)
    {
        // Approximate $0.026 per km saved
        return Math.Round(distanceSavedKm * 0.026, 2);
    }

    public static double EstimateCo2Saved(double distanceSavedKm)
    {
        // Approximate 0.0037 kg CO2 per km for ground shipping
        return Math.Round(distanceSavedKm * 0.0037, 2);
    }
}
