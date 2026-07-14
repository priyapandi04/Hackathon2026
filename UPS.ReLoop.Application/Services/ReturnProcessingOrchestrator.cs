namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Decision;
using UPS.ReLoop.Application.DTOs.ImageValidation;
using UPS.ReLoop.Application.DTOs.Integration;
using UPS.ReLoop.Application.DTOs.MatchAgent;
using UPS.ReLoop.Application.DTOs.RootCauseAgent;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;

/// <summary>
/// Orchestrates the complete return processing pipeline across all AI agents and stored procedures.
/// Flow: CreateReturnRequest -> PolicyCompliance -> HoldingClock -> ImageValidation ->
///       InventoryPool -> MatchAgent -> DiversionAgent -> RootCause -> ConfidenceGate ->
///       Revenue -> Response
/// </summary>
public class ReturnProcessingOrchestrator : IReturnProcessingOrchestrator
{
    private const decimal DefaultBasePrice = 2499m;
    private const decimal AvgReverseFreight = 180m; // illustrative reverse-parcel cost avoided (INR)

    private readonly IReturnRequestSpRepository _returnRequestSpRepo;
    private readonly IImageValidationService _imageValidationService;
    private readonly IImageValidationSpRepository _imageValidationSpRepo;
    private readonly IInventoryPoolSpRepository _inventoryPoolSpRepo;
    private readonly IMatchAgentService _matchAgentService;
    private readonly IRootCauseAgentService _rootCauseAgentService;
    private readonly IRetailerPolicyService _retailerPolicyService;
    private readonly IHoldingClockService _holdingClockService;
    private readonly IDiversionAgentService _diversionAgentService;
    private readonly AutoApprovalMetrics _autoApprovalMetrics;
    private readonly ILogger<ReturnProcessingOrchestrator> _logger;

    public ReturnProcessingOrchestrator(
        IReturnRequestSpRepository returnRequestSpRepo,
        IImageValidationService imageValidationService,
        IImageValidationSpRepository imageValidationSpRepo,
        IInventoryPoolSpRepository inventoryPoolSpRepo,
        IMatchAgentService matchAgentService,
        IRootCauseAgentService rootCauseAgentService,
        IRetailerPolicyService retailerPolicyService,
        IHoldingClockService holdingClockService,
        IDiversionAgentService diversionAgentService,
        AutoApprovalMetrics autoApprovalMetrics,
        ILogger<ReturnProcessingOrchestrator> logger)
    {
        _returnRequestSpRepo = returnRequestSpRepo;
        _imageValidationService = imageValidationService;
        _imageValidationSpRepo = imageValidationSpRepo;
        _inventoryPoolSpRepo = inventoryPoolSpRepo;
        _matchAgentService = matchAgentService;
        _rootCauseAgentService = rootCauseAgentService;
        _retailerPolicyService = retailerPolicyService;
        _holdingClockService = holdingClockService;
        _diversionAgentService = diversionAgentService;
        _autoApprovalMetrics = autoApprovalMetrics;
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
        _logger.LogInformation("Step 1 complete � ReturnRequest created: {ReturnRequestId}, Status: {Status}",
            createResult.ReturnRequestId, createResult.Status);

        // ???????????????????????????????????????????????????
        // STEP 1a: Deterministic 10-day holding clock (core UPS constraint)
        // ???????????????????????????????????????????????????
        response.HoldingClock = request.HoldingDaysCompleted is { } days
            ? _holdingClockService.EvaluateFromDays(days)
            : request.PickupDate is { } pickup
                ? _holdingClockService.Evaluate(pickup)
                : _holdingClockService.EvaluateFromDays(0);

        _logger.LogInformation("Step 1a � Holding clock: {Message}", response.HoldingClock.Message);

        // ???????????????????????????????????????????????????
        // STEP 1b: Policy-first compliance grounding (block overrides condition)
        // ???????????????????????????????????????????????????
        var policy = _retailerPolicyService.Evaluate(request.Category);
        response.PolicyCompliance = policy;
        response.Citations.Add(_retailerPolicyService.GetCitation(request.Category));

        if (policy.IsRestrictedCategory)
        {
            response.Status = "ReturnToSeller";
            response.Diversion = _diversionAgentService.Decide(0, response.HoldingClock, ResolveBasePrice(request), resaleAllowed: false);
            response.DecisionConfidence = DecisionConfidenceEvaluator.Evaluate(null, 0, policyResolved: true, policyRestricted: true);
            response.AutoApproval = AutoApprovalPolicy.Evaluate(
                response.DecisionConfidence, ResolveBasePrice(request), policyRestricted: true, clockExpired: false,
                stableKey: response.ReturnRequestId.ToString());
            _autoApprovalMetrics.Record(response.AutoApproval);
            response.ProcessedAt = DateTime.UtcNow;

            _logger.LogInformation("Step 1b � Category '{Category}' is policy-restricted ({PolicyRef}); routing back to seller.",
                request.Category, policy.PolicyRef);

            return ApiResponse<ReturnProcessingResponse>.SuccessResponse(response,
                $"Return routed back to seller — retailer policy {policy.PolicyRef} prohibits local resale.");
        }

        // If the holding window already elapsed, auto-return before doing resale work.
        if (response.HoldingClock.IsExpired)
        {
            response.Status = "ReturnToSeller";
            response.Diversion = _diversionAgentService.Decide(0, response.HoldingClock, ResolveBasePrice(request), resaleAllowed: true);
            response.DecisionConfidence = DecisionConfidenceEvaluator.Evaluate(null, 0, policyResolved: true, policyRestricted: false);
            response.AutoApproval = AutoApprovalPolicy.Evaluate(
                response.DecisionConfidence, ResolveBasePrice(request), policyRestricted: false, clockExpired: true,
                stableKey: response.ReturnRequestId.ToString());
            _autoApprovalMetrics.Record(response.AutoApproval);
            response.ProcessedAt = DateTime.UtcNow;

            _logger.LogInformation("Step 1b � Holding window elapsed at day {Day}; auto-returning to seller.",
                response.HoldingClock.HoldingDay);

            return ApiResponse<ReturnProcessingResponse>.SuccessResponse(response,
                "Return routed back to seller — 10-day holding window elapsed.");
        }

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
                        _logger.LogInformation("Step 2 � Return REJECTED by image validation for PackageId: {PackageId}", request.PackageId);
                        return ApiResponse<ReturnProcessingResponse>.SuccessResponse(response,
                            "Return rejected � item not eligible based on image validation.");
                    }

                    _logger.LogInformation("Step 2 complete � Image validated. Condition: {Condition}, Eligible: {Eligible}",
                        imageResult.Data.Condition, imageResult.Data.Eligible);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Step 2 � Image validation failed, continuing pipeline for PackageId: {PackageId}", request.PackageId);
            }
        }
        else
        {
            _logger.LogInformation("Step 2 skipped � No image provided for PackageId: {PackageId}", request.PackageId);
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
                // Must create a Returns record first so the FK
                // InventoryPool.ReturnId -> Returns.Id is satisfied.
                var returnId = await _imageValidationSpRepo.SaveResultAsync(new ImageValidationResultParams(
                    ProductId: request.PackageId.ToString(),
                    ProductName: request.ProductName,
                    Category: request.Category,
                    ReturnReason: request.ReturnReason,
                    Condition: "Unknown",
                    Eligibility: "Assumed Eligible",
                    Confidence: 0.5,
                    Location: request.Location),
                    cancellationToken);

                if (returnId != Guid.Empty)
                {
                    await _inventoryPoolSpRepo.AddToPoolAsync(
                        returnId,                          // Returns.Id  ← correct FK parent
                        request.PackageId.ToString(),
                        request.Location,
                        50.0,
                        cancellationToken);

                    _logger.LogInformation(
                        "Step 3 complete — Created Returns record {ReturnId} and added to InventoryPool (no-image flow)",
                        returnId);
                }
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
        // STEP 4: Match Agent � usp_GetInventoryByProduct, usp_GetDemandHistory, usp_SaveMatchResult
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
                    condition,
                    ReturnRequestId: response.ReturnRequestId),
                cancellationToken);

            if (matchResult.Success && matchResult.Data is not null)
            {
                response.HyperlocalMatch = new MatchResult
                {
                    MatchScore = matchResult.Data.MatchScore,
                    Recommendation = matchResult.Data.Recommendation,
                    Confidence = matchResult.Data.Confidence,
                    Explanation = matchResult.Data.Explanation,
                    Channel = matchResult.Data.Channel,
                    ExpectedDaysToSell = matchResult.Data.ExpectedDaysToSell
                };

                response.Savings = new SavingsSummary
                {
                    DistanceSavedKm = matchResult.Data.DistanceSavedKm,
                    CostSaved = (decimal)matchResult.Data.CostSaved,
                    Co2SavedKg = matchResult.Data.Co2Saved
                };

                response.Status = matchResult.Data.MatchScore >= 70 ? "Matched" : "Eligible";

                _logger.LogInformation("Step 4 complete � MatchScore: {Score}, Recommendation: {Rec}, DistanceSaved: {Dist}km",
                    matchResult.Data.MatchScore, matchResult.Data.Recommendation, matchResult.Data.DistanceSavedKm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Step 4 � Match agent failed for PackageId: {PackageId}", request.PackageId);
            response.Status = "Eligible";
        }

        // ???????????????????????????????????????????????????
        // STEP 4a: Diversion / Dynamic-Pricing agent (the diversion flywheel)
        // Turns the holding window into a countdown-to-sale.
        // ???????????????????????????????????????????????????
        var matchScore = response.HyperlocalMatch?.MatchScore ?? 0;
        response.Diversion = _diversionAgentService.Decide(
            matchScore, response.HoldingClock!, ResolveBasePrice(request), resaleAllowed: true);

        response.Status = response.Diversion.Action switch
        {
            "SELL_LOCAL" => "Matched",
            "RETURN_TO_SELLER" => "ReturnToSeller",
            "ESCALATE" => "Escalate",
            _ => response.Status
        };

        _logger.LogInformation("Step 4a — Diversion action: {Action} at Rs.{Price}",
            response.Diversion.Action, response.Diversion.SuggestedPrice);

        // ???????????????????????????????????????????????????
        // STEP 4b: reserve_item + create_listing (playbook Agent 1 actions)
        // Pre-commit the item to a local channel whenever diversion keeps it local,
        // so the item is never shipped back once a local outlet is chosen.
        // ???????????????????????????????????????????????????
        if (response.Diversion.Action is "SELL_LOCAL" or "WIDEN_RADIUS" or "DISCOUNT_SELL" or "OFFER_ACCESS_POINTS")
        {
            response.Listing = new LocalListing
            {
                Reserved = matchScore >= 40,
                Channel = string.IsNullOrEmpty(response.HyperlocalMatch?.Channel)
                    ? MatchCalculator.ResolveChannel(request.Location, matchScore)
                    : response.HyperlocalMatch!.Channel,
                ListingReference = $"LST-{response.ReturnRequestId.ToString()[..8].ToUpperInvariant()}",
                ExpectedDaysToSell = response.HyperlocalMatch?.ExpectedDaysToSell ?? MatchCalculator.EstimateDaysToSell(matchScore),
                ListedPrice = response.Diversion.SuggestedPrice
            };

            _logger.LogInformation("Step 4b — Local listing {Ref} at {Channel} (reserved={Reserved}, Rs.{Price}, ~{Days}d)",
                response.Listing.ListingReference, response.Listing.Channel, response.Listing.Reserved,
                response.Listing.ListedPrice, response.Listing.ExpectedDaysToSell);
        }

        // ???????????????????????????????????????????????????
        // STEP 5: Root Cause Agent � Azure OpenAI + usp_SaveRootCauseAnalysis
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

                _logger.LogInformation("Step 5 complete � RootCause: {RootCause}", rootCauseResult.Data.AiAnalysis.RootCause);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Step 5 � Root cause analysis failed for PackageId: {PackageId}", request.PackageId);
        }

        // ???????????????????????????????????????????????????
        // STEP 5a: Calibrated-trust confidence gate (escalate when unsure)
        // ???????????????????????????????????????????????????
        response.DecisionConfidence = DecisionConfidenceEvaluator.Evaluate(
            response.ImageValidation?.Confidence,
            response.HyperlocalMatch?.Confidence ?? 0,
            policyResolved: response.PolicyCompliance is { PolicyRef: not "RP-DEF-0.0" },
            policyRestricted: false);

        if (response.DecisionConfidence.ShouldEscalate && response.Status is not ("Matched" or "ReturnToSeller"))
        {
            response.Status = "Escalate";
            _logger.LogInformation("Step 5a � Low confidence {Score}; escalating to human review.",
                response.DecisionConfidence.Score);
        }
        // ???????????????????????????????????????????????????
        // STEP 5c: Straight-through auto-approval routing (throughput at scale)
        // Only the uncertain / high-value tail reaches the manual accept/modify/reject queue.
        // ???????????????????????????????????????????????????
        response.AutoApproval = AutoApprovalPolicy.Evaluate(
            response.DecisionConfidence,
            ResolveBasePrice(request),
            policyRestricted: false,
            clockExpired: false,
            stableKey: response.ReturnRequestId.ToString());
        _autoApprovalMetrics.Record(response.AutoApproval);        _logger.LogInformation("Step 5c \u2014 Auto-approval route: {Route} ({Reason})",
            response.AutoApproval.Route, response.AutoApproval.Reason);
        // ???????????????????????????????????????????????????
        // STEP 5b: Triple-value + new-revenue economics
        // ???????????????????????????????????????????????????
        response.RevenueOpportunity = RevenueCalculator.Calculate(
            freightAvoided: AvgReverseFreight,
            salePrice: response.Diversion?.SuggestedPrice ?? ResolveBasePrice(request),
            co2SavedKg: response.Savings.Co2SavedKg);

        // Add a precedent citation when there is a real local match (grounding).
        if (matchScore >= 40)
            response.Citations.Add(new Citation("precedent", response.ReturnRequestId.ToString(),
                $"Local demand match score {matchScore} supports resale."));

        // ???????????????????????????????????????????????????
        // STEP 6: Return complete response
        // ???????????????????????????????????????????????????
        if (string.IsNullOrEmpty(response.Status))
            response.Status = "Eligible";

        response.ProcessedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Pipeline complete for PackageId: {PackageId} — Status: {Status}, MatchScore: {Score}, Savings: Rs.{Cost}, NetValue: Rs.{Net}",
            request.PackageId, response.Status,
            response.HyperlocalMatch?.MatchScore ?? 0,
            response.Savings.CostSaved,
            response.RevenueOpportunity?.TotalNetValue ?? 0);

        return ApiResponse<ReturnProcessingResponse>.SuccessResponse(response, "Return processed successfully through all agents.");
    }

    private static decimal ResolveBasePrice(ReturnProcessingRequest request) =>
        request.BasePrice is { } p and > 0 ? p : DefaultBasePrice;
}
