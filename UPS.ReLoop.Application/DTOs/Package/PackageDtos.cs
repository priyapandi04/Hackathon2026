namespace UPS.ReLoop.Application.DTOs.Package;

public record CreatePackageDto(
    string TrackingNumber,
    string SenderName,
    string SenderAddress,
    string RecipientName,
    string RecipientAddress,
    decimal Weight,
    bool IsReturnable);

public record UpdatePackageDto(
    string? SenderName,
    string? SenderAddress,
    string? RecipientName,
    string? RecipientAddress,
    decimal? Weight,
    string? Status,
    bool? IsReturnable);

public record PackageResponseDto(
    Guid Id,
    string TrackingNumber,
    string SenderName,
    string SenderAddress,
    string RecipientName,
    string RecipientAddress,
    decimal Weight,
    string Status,
    string? AiRecommendation,
    bool IsReturnable,
    DateTime CreatedAt);
