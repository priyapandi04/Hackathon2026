namespace UPS.ReLoop.Application.DTOs.ImageValidation;

public record ImageValidationRequest(
    string ImageBase64,
    string? ProductId = null,
    string? ProductName = null,
    string? ProductCategory = null,
    string? ReturnReason = null,
    string? Location = null,
    string? AdditionalContext = null);
