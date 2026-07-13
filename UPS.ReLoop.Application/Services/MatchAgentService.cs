namespace UPS.ReLoop.Application.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.MatchAgent;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;
using UPS.ReLoop.Domain.Entities;
using UPS.ReLoop.Domain.Interfaces;

public class MatchAgentService : IMatchAgentService
{
    private readonly IInventoryPoolRepository _inventoryRepo;
    private readonly IDemandHistoryRepository _demandRepo;
    private readonly IOpenAIService _openAIService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryPoolSpRepository _inventoryPoolSpRepo;
    private readonly IDemandHistorySpRepository _demandHistorySpRepo;
    private readonly IMatchResultSpRepository _matchResultSpRepo;
    private readonly ILogger<MatchAgentService> _logger;

    public MatchAgentService(
        IInventoryPoolRepository inventoryRepo,
        IDemandHistoryRepository demandRepo,
        IOpenAIService openAIService,
        IUnitOfWork unitOfWork,
        IInventoryPoolSpRepository inventoryPoolSpRepo,
        IDemandHistorySpRepository demandHistorySpRepo,
        IMatchResultSpRepository matchResultSpRepo,
        ILogger<MatchAgentService> logger)
    {
        _inventoryRepo = inventoryRepo;
        _demandRepo = demandRepo;
        _openAIService = openAIService;
        _unitOfWork = unitOfWork;
        _inventoryPoolSpRepo = inventoryPoolSpRepo;
        _demandHistorySpRepo = demandHistorySpRepo;
        _matchResultSpRepo = matchResultSpRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<MatchAgentResponse>> FindLocalMatchAsync(MatchAgentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finding local match for product {ProductId} in {Location}", request.ProductId, request.Location);

        try
        {
            // Step 1: Get inventory by product via SP
            var inventoryItems = await _inventoryPoolSpRepo.GetByProductAsync(request.ProductId, request.Location, cancellationToken);

            // Step 2: Get demand history via SP (product-specific) + regional demand (grounded market signal)
            var demandItems = await _demandHistorySpRepo.GetAsync(request.ProductId, request.Location, cancellationToken);
            var regionalDemand = await _demandRepo.GetByRegionAsync(request.Location, cancellationToken);

            // Step 3: Calculate match score, distance saved, cost saved, CO2 saved
            var (matchScore, details) = CalculateMatchScore(request, inventoryItems, demandItems, regionalDemand);
            var recommendation = MatchCalculator.DetermineRecommendation(matchScore);
            var confidence = MatchCalculator.CalculateConfidence(matchScore, details.Count);
            var distanceSaved = MatchCalculator.EstimateDistanceSaved(matchScore);
            var costSaved = MatchCalculator.EstimateCostSaved(distanceSaved);
            var co2Saved = MatchCalculator.EstimateCo2Saved(distanceSaved);

            // Generate AI explanation
            var explanation = await GenerateExplanationAsync(request, matchScore, recommendation, details, cancellationToken);

            var response = new MatchAgentResponse
            {
                MatchScore = matchScore,
                Recommendation = recommendation,
                Confidence = confidence,
                DistanceSavedKm = distanceSaved,
                CostSaved = costSaved,
                Co2Saved = co2Saved,
                Explanation = explanation,
                Channel = MatchCalculator.ResolveChannel(request.Location, matchScore),
                ExpectedDaysToSell = MatchCalculator.EstimateDaysToSell(matchScore),
                MatchDetails = details
            };

            // Step 4: Save match result via SP
            await SaveMatchResultAsync(request, response, details, cancellationToken);

            _logger.LogInformation("Match result for {ProductId}: Score={Score}, Recommendation={Recommendation}",
                request.ProductId, matchScore, recommendation);

            return ApiResponse<MatchAgentResponse>.SuccessResponse(response, "Hyperlocal match analysis completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during match analysis for product {ProductId}", request.ProductId);
            return ApiResponse<MatchAgentResponse>.FailResponse("Match analysis failed. Please retry.", 500);
        }
    }

    private static (int Score, List<MatchDetail> Details) CalculateMatchScore(
        MatchAgentRequest request,
        IReadOnlyList<InventoryItemDto> inventoryItems,
        IReadOnlyList<DemandHistoryDto> demandItems,
        IReadOnlyList<DemandHistory> regionalDemand)
    {
        var details = new List<MatchDetail>();
        int totalScore = 0;

        // Factor 1: Category resale velocity (0-45) — hyperlocal demand model.
        // Lets the agent score products it has never seen before, not only seeded SKUs.
        var categoryIndex = MatchCalculator.CategoryDemandIndex(request.Category);
        var categoryPoints = (int)Math.Round(categoryIndex / 100.0 * 45);
        totalScore += categoryPoints;
        details.Add(new MatchDetail
        {
            Factor = "Category Demand",
            Points = categoryPoints,
            Reason = $"'{request.Category}' resale-velocity index {categoryIndex}/100 in the local demand model"
        });

        // Factor 2: Regional demand strength (0-25) — grounded in DemandHistory for the hub.
        var regionScore = regionalDemand.Count > 0 ? regionalDemand.Average(d => d.DemandScore) : 0.0;
        var regionPoints = (int)Math.Round(Math.Clamp(regionScore, 0, 100) / 100.0 * 25);
        if (regionPoints > 0)
        {
            totalScore += regionPoints;
            details.Add(new MatchDetail
            {
                Factor = "Regional Demand",
                Points = regionPoints,
                Reason = $"Avg local demand {regionScore:F0}/100 across {regionalDemand.Count} SKUs in '{request.Location}'"
            });
        }

        // Factor 3: Condition (0-20).
        var conditionPoints = MatchCalculator.ConditionResaleScore(request.Condition, 20);
        totalScore += conditionPoints;
        details.Add(new MatchDetail
        {
            Factor = "Condition",
            Points = conditionPoints,
            Reason = $"Reported condition '{request.Condition}' resale-graded at {conditionPoints}/20"
        });

        // Factor 4: Exact local match bonus (0-10) — strong signal when the same SKU
        // is already pooled locally or already proven in this hub's demand history.
        if (inventoryItems.Any(i => i.ProductId == request.ProductId))
        {
            totalScore += 6;
            details.Add(new MatchDetail
            {
                Factor = "Local Inventory",
                Points = 6,
                Reason = $"Product '{request.ProductId}' already pooled locally"
            });
        }

        var maxProductDemand = demandItems
            .Where(d => d.ProductId == request.ProductId)
            .Select(d => d.DemandScore)
            .DefaultIfEmpty(0)
            .Max();

        if (maxProductDemand >= 70.0)
        {
            totalScore += 4;
            details.Add(new MatchDetail
            {
                Factor = "Proven SKU Demand",
                Points = 4,
                Reason = $"This SKU already scores demand {maxProductDemand:F0} in-region"
            });
        }

        return (Math.Clamp(totalScore, 0, 100), details);
    }

    private async Task SaveMatchResultAsync(
        MatchAgentRequest request,
        MatchAgentResponse response,
        List<MatchDetail> details,
        CancellationToken cancellationToken)
    {
        try
        {
            var returnRequestId = Guid.TryParse(request.ProductId, out var parsed) ? parsed : Guid.Empty;

            // find-match is a what-if preview: with no real ReturnRequest to link to,
            // persisting would violate the MatchAgentResults→ReturnRequests FK and flood
            // the table with orphan rows. Skip the save unless a valid return id is present.
            if (returnRequestId == Guid.Empty)
            {
                _logger.LogDebug("Preview match for {ProductId} not persisted (no linked ReturnRequest).", request.ProductId);
                return;
            }

            // Persist the full triple-value economics (INR) alongside the match so the
            // historical dashboard aggregate reflects real net value, not freight only.
            var econ = RevenueCalculator.Calculate(
                freightAvoided: (decimal)response.CostSaved,
                salePrice: request.SalePrice,
                co2SavedKg: response.Co2Saved);

            await _matchResultSpRepo.SaveAsync(new SaveMatchResultParams(
                ReturnRequestId: returnRequestId,
                ProductId: request.ProductId,
                ProductName: request.ProductName,
                Category: request.Category,
                Location: request.Location,
                Condition: request.Condition,
                MatchScore: response.MatchScore,
                Recommendation: response.Recommendation,
                Confidence: response.Confidence,
                DistanceSavedKm: response.DistanceSavedKm,
                CostSaved: response.CostSaved,
                Co2Saved: response.Co2Saved,
                SalePrice: request.SalePrice,
                ResaleMargin: econ.ResaleMargin,
                ResaleServiceFee: econ.ResaleServiceFee,
                Co2Value: econ.Co2ValueInr,
                NetValue: econ.TotalNetValue,
                Explanation: response.Explanation,
                MatchDetailsJson: JsonSerializer.Serialize(details)
            ), cancellationToken);

            _logger.LogInformation("Match result saved via SP for ProductId: {ProductId}, Score: {Score}",
                request.ProductId, response.MatchScore);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save match result via SP for ProductId: {ProductId}", request.ProductId);
        }
    }

    private async Task<string> GenerateExplanationAsync(
        MatchAgentRequest request,
        int matchScore,
        string recommendation,
        List<MatchDetail> details,
        CancellationToken cancellationToken)
    {
        try
        {
            var factorsSummary = string.Join("; ", details.Select(d => $"{d.Factor}: {d.Reason}"));

            var prompt = $"""
                You are a supply chain optimization AI for UPS ReLoop Nexus.
                Provide a brief (2-3 sentence) business explanation for the following match decision:

                Product: {request.ProductName} (Category: {request.Category})
                Location: {request.Location}
                Condition: {request.Condition}
                Match Score: {matchScore}/100
                Recommendation: {recommendation}
                Contributing Factors: {factorsSummary}

                Explain why this recommendation makes business sense for sustainability and cost efficiency.
                """;

            return await _openAIService.GenerateTextAsync(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI explanation, using fallback");
            return $"Match score of {matchScore} with recommendation '{recommendation}' based on {details.Count} contributing factors.";
        }
    }
}
