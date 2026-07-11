namespace UPS.ReLoop.Application.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.BusinessExplanation;
using UPS.ReLoop.Application.Interfaces;

public class BusinessExplanationService : IBusinessExplanationService
{
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<BusinessExplanationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BusinessExplanationService(IOpenAIService openAIService, ILogger<BusinessExplanationService> logger)
    {
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<ApiResponse<BusinessExplanationResponse>> GenerateExplanationAsync(BusinessExplanationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating business explanation for product: {ProductName}", request.ProductName);

        try
        {
            var prompt = BusinessExplanationPromptBuilder.Build(request);
            var rawResponse = await _openAIService.GenerateTextAsync(prompt, cancellationToken);

            var response = ParseResponse(rawResponse);

            _logger.LogInformation("Business explanation generated successfully for {ProductName}", request.ProductName);
            return ApiResponse<BusinessExplanationResponse>.SuccessResponse(response, "Business explanation generated successfully");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response for {ProductName}", request.ProductName);
            return ApiResponse<BusinessExplanationResponse>.FailResponse("Failed to parse explanation response. Please retry.", 502);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "AI service error for {ProductName}", request.ProductName);
            return ApiResponse<BusinessExplanationResponse>.FailResponse("AI service is temporarily unavailable.", 503);
        }
    }

    private static BusinessExplanationResponse ParseResponse(string rawJson)
    {
        var json = rawJson.Trim();
        if (json.StartsWith("```"))
        {
            var firstNewline = json.IndexOf('\n');
            var lastFence = json.LastIndexOf("```");
            if (firstNewline > 0 && lastFence > firstNewline)
                json = json[(firstNewline + 1)..lastFence].Trim();
        }

        return JsonSerializer.Deserialize<BusinessExplanationResponse>(json, JsonOptions)
            ?? throw new JsonException("Deserialization returned null.");
    }
}
