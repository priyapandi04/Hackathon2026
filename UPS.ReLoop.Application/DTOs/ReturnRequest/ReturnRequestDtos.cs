namespace UPS.ReLoop.Application.DTOs.ReturnRequest;

public record CreateReturnRequestDto(
    Guid PackageId,
    string Reason,
    string? Location = null,
    string? ImageUrl = null);

public record ReturnRequestResponseDto(
    Guid Id,
    Guid PackageId,
    string Reason,
    string Status,
    string? AiAnalysis,
    string? ResolutionNotes,
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public record CreateReturnRequestSpResponse(
    Guid ReturnRequestId,
    Guid PackageId,
    string Status,
    DateTime CreatedDate);
