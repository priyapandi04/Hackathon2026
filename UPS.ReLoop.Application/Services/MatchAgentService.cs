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

            // Step 2: Get demand history via SP
            var demandItems = await _demandHistorySpRepo.GetAsync(request.ProductId, request.Location, cancellationToken);

            // Step 3: Calculate match score, distance saved, cost saved, CO2 saved
            var (matchScore, details) = CalculateMatchScore(request, inventoryItems, demandItems);
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
        IReadOnlyList<DemandHistoryDto> demandItems)
    {
        var details = new List<MatchDetail>();
        int totalScore = 0;

        // Rule 1: Same Product match in local inventory
        var productMatch = inventoryItems.Any(i => i.ProductId == request.ProductId);
        if (productMatch)
        {
            totalScore += 50;
            details.Add(new MatchDetail
            {
                Factor = "Same Product",
                Points = 50,
                Reason = $"Product '{request.ProductId}' found in local inventory pool"
            });
        }

        // Rule 2: Same Area demand
        var areaMatch = demandItems.Any(d =>
            d.Region.Equals(request.Location, StringComparison.OrdinalIgnoreCase));
        if (areaMatch)
        {
            totalScore += 20;
            details.Add(new MatchDetail
            {
                Factor = "Same Area",
                Points = 20,
                Reason = $"Active demand detected in region '{request.Location}'"
            });
        }

        // Rule 3: High Demand Score
        var maxDemandScore = demandItems
            .Where(d => d.ProductId == request.ProductId)
            .Select(d => d.DemandScore)
            .DefaultIfEmpty(0)
            .Max();

        if (maxDemandScore >= 70.0)
        {
            totalScore += 20;
            details.Add(new MatchDetail
            {
                Factor = "High Demand",
                Points = 20,
                Reason = $"Demand score {maxDemandScore:F1} exceeds threshold of 70"
            });
        }

        // Rule 4: Good Condition
        var goodConditions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "New", "Like New", "Good", "Excellent", "Refurbished" };

        if (goodConditions.Contains(request.Condition))
        {
            totalScore += 10;
            details.Add(new MatchDetail
            {
                Factor = "Good Condition",
                Points = 10,
                Reason = $"Product condition '{request.Condition}' qualifies for resale"
            });
        }

        return (Math.Min(totalScore, 100), details);
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
