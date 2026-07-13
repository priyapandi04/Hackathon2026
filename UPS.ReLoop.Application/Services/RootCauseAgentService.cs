namespace UPS.ReLoop.Application.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.Common.Exceptions;
using UPS.ReLoop.Application.DTOs.RootCauseAgent;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class RootCauseAgentService : IRootCauseAgentService
{
    private readonly IOpenAIService _openAIService;
    private readonly IRootCauseSpRepository _rootCauseSpRepo;
    private readonly ILogger<RootCauseAgentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RootCauseAgentService(
        IOpenAIService openAIService,
        IRootCauseSpRepository rootCauseSpRepo,
        ILogger<RootCauseAgentService> logger)
    {
        _openAIService = openAIService;
        _rootCauseSpRepo = rootCauseSpRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<RootCauseAnalysisResult>> AnalyzeAsync(RootCauseRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Returns is null || request.Returns.Count == 0)
            throw new BadRequestException("At least one return item is required for analysis.");

        _logger.LogInformation("Starting root cause analysis for {Count} returns", request.Returns.Count);

        try
        {
            // Step 1: Retrieve historical return reasons via usp_GetReturnReasonsByCategory
            var category = request.Returns
                .GroupBy(r => r.Category, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .First().Key;

            var historicalReasons = await _rootCauseSpRepo.GetReturnReasonsByCategoryAsync(category, cancellationToken);

            // Use SP data if available, else fall back to request data
            var breakdown = historicalReasons.Any()
                ? historicalReasons.Select(r => new ReasonBreakdown
                {
                    Reason = r.ReturnReason,
                    Count = r.Count,
                    Percentage = r.Percentage
                }).ToList()
                : request.Returns
                    .GroupBy(r => r.ReturnReason, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new ReasonBreakdown
                    {
                        Reason = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / request.Returns.Count * 100, 1)
                    })
                    .OrderByDescending(b => b.Count)
                    .ToList();

            var topLocation = request.Returns
                .GroupBy(r => r.Location, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .First().Key;

            // Step 2 & 3: Build prompt and analyze via Azure OpenAI
            var prompt = RootCausePromptBuilder.Build(request, breakdown);
            var rawResponse = await _openAIService.GenerateTextAsync(prompt, cancellationToken);

            _logger.LogDebug("Raw AI response: {Response}", rawResponse);

            var aiAnalysis = ParseResponse(rawResponse);

            // Step 5: Save output via usp_SaveRootCauseAnalysis
            await SaveAnalysisAsync(aiAnalysis, cancellationToken);

            var result = new RootCauseAnalysisResult
            {
                AiAnalysis = aiAnalysis,
                ReasonBreakdown = breakdown,
                TotalReturns = historicalReasons.Any() ? historicalReasons.Sum(r => r.Count) : request.Returns.Count,
                TopCategory = category,
                TopLocation = topLocation
            };

            _logger.LogInformation("Root cause analysis completed: {RootCause}", aiAnalysis.RootCause);

            return ApiResponse<RootCauseAnalysisResult>.SuccessResponse(result, "Root cause analysis completed successfully");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI root cause response");
            return ApiResponse<RootCauseAnalysisResult>.FailResponse("Failed to parse AI analysis response. Please retry.", 502);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "AI service error during root cause analysis");
            return ApiResponse<RootCauseAnalysisResult>.FailResponse("AI service is temporarily unavailable.", 503);
        }
    }

    /// <summary>
    /// Deterministic clustering of many returns into systemic root causes, each
    /// turned into a priced retailer fix-ticket. No LLM call — pure aggregation —
    /// so the numbers are auditable.
    /// </summary>
    public ApiResponse<ReturnClusterResult> ClusterReturns(RootCauseRequest request, decimal avgReverseCostPerItem = 180m)
    {
        if (request.Returns is null || request.Returns.Count == 0)
            throw new BadRequestException("At least one return item is required for clustering.");

        var total = request.Returns.Count;

        var clusters = request.Returns
            .GroupBy(r => r.Category, StringComparer.OrdinalIgnoreCase)
            .Select(catGroup =>
            {
                var count = catGroup.Count();
                var dominant = catGroup
                    .GroupBy(r => r.ReturnReason, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .First();
                var topLocation = catGroup
                    .GroupBy(r => r.Location, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                var pct = Math.Round((double)dominant.Count() / count * 100, 1);
                // Illustrative annualised impact: dominant-reason volume scaled to a
                // year (x260 operating days) x avg reverse cost avoided if fixed.
                var annualImpact = Math.Round(dominant.Count() * 260m * avgReverseCostPerItem, 0);

                return new ReturnCluster
                {
                    Category = catGroup.Key,
                    DominantReason = dominant.Key,
                    Count = count,
                    Percentage = pct,
                    TopLocation = topLocation,
                    EstimatedAnnualImpact = annualImpact,
                    FixTicket = $"{pct:0}% of {catGroup.Key} returns trace to '{dominant.Key}' " +
                                $"(hotspot: {topLocation}). Projected ~\u20b9{annualImpact:N0}/yr if fixed at source."
                };
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        var result = new ReturnClusterResult { TotalReturns = total, Clusters = clusters };
        _logger.LogInformation("Clustered {Total} returns into {Count} systemic causes.", total, clusters.Count);
        return ApiResponse<ReturnClusterResult>.SuccessResponse(result, "Return clustering completed.");
    }

    private async Task SaveAnalysisAsync(RootCauseResponse aiAnalysis, CancellationToken cancellationToken)
    {
        try
        {
            await _rootCauseSpRepo.SaveAnalysisAsync(new SaveRootCauseParams(
                RootCause: aiAnalysis.RootCause,
                Frequency: aiAnalysis.Frequency,
                Recommendation: aiAnalysis.Recommendation,
                Impact: aiAnalysis.Impact,
                Confidence: 0.85
            ), cancellationToken);

            _logger.LogInformation("Root cause analysis saved via SP: {RootCause}", aiAnalysis.RootCause);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save root cause analysis � continuing");
        }
    }

    private static RootCauseResponse ParseResponse(string rawJson)
    {
        var json = rawJson.Trim();
        if (json.StartsWith("```"))
        {
            var firstNewline = json.IndexOf('\n');
            var lastFence = json.LastIndexOf("```");
            if (firstNewline > 0 && lastFence > firstNewline)
                json = json[(firstNewline + 1)..lastFence].Trim();
        }

        return JsonSerializer.Deserialize<RootCauseResponse>(json, JsonOptions)
            ?? throw new JsonException("Deserialization returned null.");
    }
}
