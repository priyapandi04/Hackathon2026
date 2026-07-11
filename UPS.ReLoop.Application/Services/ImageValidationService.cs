namespace UPS.ReLoop.Application.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.ImageValidation;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class ImageValidationService : IImageValidationService
{
    private readonly IOpenAIService _openAIService;
    private readonly IImageValidationSpRepository _imageValidationSpRepo;
    private readonly IInventoryPoolSpRepository _inventoryPoolSpRepo;
    private readonly ILogger<ImageValidationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ImageValidationService(
        IOpenAIService openAIService,
        IImageValidationSpRepository imageValidationSpRepo,
        IInventoryPoolSpRepository inventoryPoolSpRepo,
        ILogger<ImageValidationService> logger)
    {
        _openAIService = openAIService;
        _imageValidationSpRepo = imageValidationSpRepo;
        _inventoryPoolSpRepo = inventoryPoolSpRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<ImageValidationResponse>> ValidateImageAsync(ImageValidationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting image validation for product category: {Category}", request.ProductCategory ?? "Unspecified");

        try
        {
            var prompt = ImageValidationPromptBuilder.Build(request.ProductCategory, request.AdditionalContext);

            var rawResponse = await _openAIService.AnalyzeImageAsync(request.ImageBase64, prompt, cancellationToken);

            _logger.LogDebug("Raw AI response: {Response}", rawResponse);

            var validationResponse = ParseResponse(rawResponse);

            _logger.LogInformation(
                "Image validation completed. Condition: {Condition}, Eligible: {Eligible}, Confidence: {Confidence}",
                validationResponse.Condition, validationResponse.Eligible, validationResponse.Confidence);

            // Step 2: Persist validation result via usp_SaveImageValidationResult
            var resultId = await PersistValidationResultAsync(request, validationResponse, cancellationToken);

            // Step 3: If eligible, add to inventory pool via usp_AddToInventoryPool
            if (validationResponse.Eligible && resultId != Guid.Empty)
            {
                await AddToInventoryPoolAsync(resultId, request, validationResponse, cancellationToken);
            }

            return ApiResponse<ImageValidationResponse>.SuccessResponse(validationResponse, "Image validation completed successfully");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON");
            return ApiResponse<ImageValidationResponse>.FailResponse(
                "Failed to parse AI validation response. Please retry.", 502);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request data");
            return ApiResponse<ImageValidationResponse>.FailResponse(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "AI service error during image validation");
            return ApiResponse<ImageValidationResponse>.FailResponse(
                "AI service is temporarily unavailable. Please retry.", 503);
        }
    }

    private async Task<Guid> PersistValidationResultAsync(ImageValidationRequest request, ImageValidationResponse response, CancellationToken cancellationToken)
    {
        try
        {
            var eligibility = response.Eligible ? "Eligible" : "Not Eligible";

            var resultId = await _imageValidationSpRepo.SaveResultAsync(new ImageValidationResultParams(
                ProductId: request.ProductId ?? "UNKNOWN",
                ProductName: request.ProductName ?? "Unknown Product",
                Category: request.ProductCategory ?? "General",
                ReturnReason: request.ReturnReason ?? "Not Specified",
                Condition: response.Condition,
                Eligibility: eligibility,
                Confidence: response.Confidence,
                Location: request.Location ?? "Unknown"
            ), cancellationToken);

            _logger.LogInformation("Image validation result persisted with Id: {ResultId}", resultId);
            return resultId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist image validation result — continuing without persistence");
            return Guid.Empty;
        }
    }

    private async Task AddToInventoryPoolAsync(Guid resultId, ImageValidationRequest request, ImageValidationResponse response, CancellationToken cancellationToken)
    {
        try
        {
            var matchScore = response.Confidence * 100;

            await _inventoryPoolSpRepo.AddToPoolAsync(
                returnId: resultId,
                productId: request.ProductId ?? "UNKNOWN",
                location: request.Location ?? "Unknown",
                matchScore: matchScore,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Eligible item added to inventory pool. ResultId: {ResultId}, MatchScore: {Score}", resultId, matchScore);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add item to inventory pool — continuing without pool update");
        }
    }

    private static ImageValidationResponse ParseResponse(string rawJson)
    {
        // Strip markdown code fences if present
        var json = rawJson.Trim();
        if (json.StartsWith("```"))
        {
            var firstNewline = json.IndexOf('\n');
            var lastFence = json.LastIndexOf("```");
            if (firstNewline > 0 && lastFence > firstNewline)
            {
                json = json[(firstNewline + 1)..lastFence].Trim();
            }
        }

        var result = JsonSerializer.Deserialize<ImageValidationResponse>(json, JsonOptions)
            ?? throw new JsonException("Deserialization returned null.");

        // Clamp values to valid ranges
        result.DamageScore = Math.Clamp(result.DamageScore, 0, 10);
        result.Confidence = Math.Clamp(result.Confidence, 0.0, 1.0);

        return result;
    }
}
