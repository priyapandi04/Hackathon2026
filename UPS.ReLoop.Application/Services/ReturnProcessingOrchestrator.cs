namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.ImageValidation;
using UPS.ReLoop.Application.DTOs.Integration;
using UPS.ReLoop.Application.DTOs.MatchAgent;
using UPS.ReLoop.Application.DTOs.RootCauseAgent;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;

/// <summary>
/// Orchestrates the complete return processing pipeline across all AI agents and stored procedures.
/// Flow: CreateReturnRequest ? ImageValidation ? InventoryPool ? MatchAgent ? RootCause ? Response
/// </summary>
public class ReturnProcessingOrchestrator : IReturnProcessingOrchestrator
{
    private readonly IReturnRequestSpRepository _returnRequestSpRepo;
    private readonly IImageValidationService _imageValidationService;
    private readonly IInventoryPoolSpRepository _inventoryPoolSpRepo;
    private readonly IMatchAgentService _matchAgentService;
    private readonly IRootCauseAgentService _rootCauseAgentService;
    private readonly ILogger<ReturnProcessingOrchestrator> _logger;

    public ReturnProcessingOrchestrator(
        IReturnRequestSpRepository returnRequestSpRepo,
        IImageValidationService imageValidationService,
        IInventoryPoolSpRepository inventoryPoolSpRepo,
        IMatchAgentService matchAgentService,
        IRootCauseAgentService rootCauseAgentService,
        ILogger<ReturnProcessingOrchestrator> logger)
    {
        _returnRequestSpRepo = returnRequestSpRepo;
        _imageValidationService = imageValidationService;
        _inventoryPoolSpRepo = inventoryPoolSpRepo;
        _matchAgentService = matchAgentService;
        _rootCauseAgentService = rootCauseAgentService;
        _logger = logger;
    }

    public async Task<ApiResponse<ReturnProcessingResponse>> ProcessReturnAsync(
        ReturnProcessingRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting return processing pipeline for PackageId: {PackageId}", request.PackageId);

        var response = new ReturnProcessingResponse
        {
            PackageId = request.PackageId
        };

        // ???????????????????????????????????????????????????
        // STEP 1: Create Return Request via usp_CreateReturnRequest
        // ???????????????????????????????????????????????????
        var createResult = await _returnRequestSpRepo.CreateAsync(
            request.PackageId, request.ReturnReason, request.Location, null, cancellationToken);

        if (createResult is null)
        {
            _logger.LogError("usp_CreateReturnRequest returned null for PackageId: {PackageId}", request.PackageId);
            return ApiResponse<ReturnProcessingResponse>.FailResponse("Failed to create return request.", 500);
        }

        response.ReturnRequestId = createResult.ReturnRequestId;
        response.Status = createResult.Status;
        _logger.LogInformation("Step 1 complete — ReturnRequest created: {ReturnRequestId}, Status: {Status}",
            createResult.ReturnRequestId, createResult.Status);

        // ???????????????????????????????????????????????????
        // STEP 2: Image Validation via Azure OpenAI + usp_SaveImageValidationResult
        // ???????????????????????????????????????????????????
        Guid imageValidationResultId = Guid.Empty;

        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            try
            {
                var imageResult = await _imageValidationService.ValidateImageAsync(
                    new ImageValidationRequest(
                        request.ImageBase64,
                        ProductId: request.PackageId.ToString(),
                        ProductName: request.ProductName,
                        ProductCategory: request.Category,
                        ReturnReason: request.ReturnReason,
                        Location: request.Location,
                        AdditionalContext: request.AdditionalContext),
                    cancellationToken);

                // ImageValidationService internally calls usp_SaveImageValidationResult
                // and usp_AddToInventoryPool if eligible

                if (imageResult.Success && imageResult.Data is not null)
                {
                    response.ImageValidation = new ImageValidationResult
                    {
                        Condition = imageResult.Data.Condition,
                        DamageScore = imageResult.Data.DamageScore,
                        Eligible = imageResult.Data.Eligible,
                        Confidence = imageResult.Data.Confidence,
                        Remarks = imageResult.Data.Remarks
                    };

                    if (!imageResult.Data.Eligible)
                    {
                        response.Status = "Rejected";
                        _logger.LogInformation("Step 2 — Return REJECTED by image validation for PackageId: {PackageId}", request.PackageId);
                        return ApiResponse<ReturnProcessingResponse>.SuccessResponse(response,
                            "Return rejected — item not eligible based on image validation.");
                    }

                    _logger.LogInformation("Step 2 complete — Image validated. Condition: {Condition}, Eligible: {Eligible}",
                        imageResult.Data.Condition, imageResult.Data.Eligible);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Step 2 — Image validation failed, continuing pipeline for PackageId: {PackageId}", request.PackageId);
            }
        }
        else
        {
            _logger.LogInformation("Step 2 skipped — No image provided for PackageId: {PackageId}", request.PackageId);
        }

        // ???????????????????????????????????????????????????
        // STEP 3: Add to Inventory Pool (if eligible) via usp_AddToInventoryPool
        // (Already handled inside ImageValidationService when Eligible=true)
        // If no image was provided, explicitly add to pool
        // ???????????????????????????????????????????????????
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            try
            {
                await _inventoryPoolSpRepo.AddToPoolAsync(
                    response.ReturnRequestId,
                    request.PackageId.ToString(),
                    request.Location,
                    50.0,
                    cancellationToken);

                _logger.LogInformation("Step 3 complete — Added to inventory pool (no image flow) for ReturnRequestId: {Id}", response.ReturnRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Step 3 — Failed to add to inventory pool for PackageId: {PackageId}", request.PackageId);
            }
        }
        else
        {
            _logger.LogInformation("Step 3 — Inventory pool handled by ImageValidationService");
        }

        // ???????????????????????????????????????????????????
        // STEP 4: Match Agent — usp_GetInventoryByProduct, usp_GetDemandHistory, usp_SaveMatchResult
        // (All handled inside MatchAgentService)
        // ???????????????????????????????????????????????????
        try
        {
            var condition = response.ImageValidation?.Condition ?? "Unknown";
            var matchResult = await _matchAgentService.FindLocalMatchAsync(
                new MatchAgentRequest(
                    request.PackageId.ToString(),
                    request.ProductName,
                    request.Category,
                    request.Location,
                    condition),
                cancellationToken);

            if (matchResult.Success && matchResult.Data is not null)
            {
                response.HyperlocalMatch = new MatchResult
                {
                    MatchScore = matchResult.Data.MatchScore,
                    Recommendation = matchResult.Data.Recommendation,
                    Confidence = matchResult.Data.Confidence,
                    Explanation = matchResult.Data.Explanation
                };

                response.Savings = new SavingsSummary
                {
                    DistanceSavedKm = matchResult.Data.DistanceSavedKm,
                    CostSaved = (decimal)matchResult.Data.CostSaved,
                    Co2SavedKg = matchResult.Data.Co2Saved
                };

                response.Status = matchResult.Data.MatchScore >= 70 ? "Matched" : "Eligible";

                _logger.LogInformation("Step 4 complete — MatchScore: {Score}, Recommendation: {Rec}, DistanceSaved: {Dist}km",
                    matchResult.Data.MatchScore, matchResult.Data.Recommendation, matchResult.Data.DistanceSavedKm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Step 4 — Match agent failed for PackageId: {PackageId}", request.PackageId);
            response.Status = "Eligible";
        }

        // ???????????????????????????????????????????????????
        // STEP 5: Root Cause Agent — Azure OpenAI + usp_SaveRootCauseAnalysis
        // (All handled inside RootCauseAgentService)
        // ???????????????????????????????????????????????????
        try
        {
            var rootCauseResult = await _rootCauseAgentService.AnalyzeAsync(
                new RootCauseRequest([new ReturnItem(request.Category, request.ProductName, request.ReturnReason, request.Location)]),
                cancellationToken);

            if (rootCauseResult.Success && rootCauseResult.Data is not null)
            {
                response.RootCauseAnalysis = new RootCauseResult
                {
                    RootCause = rootCauseResult.Data.AiAnalysis.RootCause,
                    Recommendation = rootCauseResult.Data.AiAnalysis.Recommendation,
                    Impact = rootCauseResult.Data.AiAnalysis.Impact
                };

                _logger.LogInformation("Step 5 complete — RootCause: {RootCause}", rootCauseResult.Data.AiAnalysis.RootCause);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Step 5 — Root cause analysis failed for PackageId: {PackageId}", request.PackageId);
        }

        // ???????????????????????????????????????????????????
        // STEP 6: Return complete response
        // ???????????????????????????????????????????????????
        if (string.IsNullOrEmpty(response.Status))
            response.Status = "Eligible";

        response.ProcessedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Pipeline complete for PackageId: {PackageId} — Status: {Status}, MatchScore: {Score}, Savings: ${Cost}",
            request.PackageId, response.Status,
            response.HyperlocalMatch?.MatchScore ?? 0,
            response.Savings.CostSaved);

        return ApiResponse<ReturnProcessingResponse>.SuccessResponse(response, "Return processed successfully through all agents.");
    }
}
