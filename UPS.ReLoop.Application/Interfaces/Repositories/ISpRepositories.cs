namespace UPS.ReLoop.Application.Interfaces.Repositories;

using UPS.ReLoop.Application.DTOs.Dashboard;
using UPS.ReLoop.Application.DTOs.ReturnRequest;

// ========================
// Repository Interfaces
// ========================

/// <summary>Aggregates live MatchAgentResults + ReturnRequests into per-segment analytics.</summary>
public interface ISegmentAnalyticsRepository
{
    Task<List<SegmentAnalyticsDto>> GetSegmentAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>Per-location aggregates (returns count, INR recovered, CO2) from live match results.</summary>
    Task<List<LocationAnalyticsDto>> GetLocationAnalyticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes usp_CreateReturnRequest and usp_GetReturnRequestById.
/// </summary>
public interface IReturnRequestSpRepository
{
    Task<CreateReturnRequestSpResponse?> CreateAsync(Guid packageId, string reason, string? location = null, string? imageUrl = null, CancellationToken cancellationToken = default);
    Task<ReturnRequestDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes usp_SaveImageValidationResult.
/// </summary>
public interface IImageValidationSpRepository
{
    Task<Guid> SaveResultAsync(ImageValidationResultParams parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes usp_AddToInventoryPool and usp_GetInventoryByProduct.
/// </summary>
public interface IInventoryPoolSpRepository
{
    Task AddToPoolAsync(Guid returnId, string productId, string location, double matchScore, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItemDto>> GetByProductAsync(string productId, string? location = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes usp_GetDemandHistory.
/// </summary>
public interface IDemandHistorySpRepository
{
    Task<IReadOnlyList<DemandHistoryDto>> GetAsync(string productId, string? region = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes usp_SaveMatchResult.
/// </summary>
public interface IMatchResultSpRepository
{
    Task<Guid> SaveAsync(SaveMatchResultParams parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes usp_SaveRootCauseAnalysis and usp_GetReturnReasonsByCategory.
/// </summary>
public interface IRootCauseSpRepository
{
    Task<Guid> SaveAnalysisAsync(SaveRootCauseParams parameters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnReasonByCategoryDto>> GetReturnReasonsByCategoryAsync(string category, CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes usp_GetDashboardMetrics and usp_GetDashboardRootCauseInsights.
/// </summary>
public interface IDashboardSpRepository
{
    Task<DashboardMetricsDto> GetMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}

// ========================
// Parameter Records
// ========================

public record ImageValidationResultParams(
    string ProductId,
    string ProductName,
    string Category,
    string ReturnReason,
    string Condition,
    string Eligibility,
    double Confidence,
    string Location);

public record SaveMatchResultParams(
    Guid ReturnRequestId,
    string ProductId,
    string ProductName,
    string Category,
    string Location,
    string Condition,
    int MatchScore,
    string Recommendation,
    double Confidence,
    double DistanceSavedKm,
    double CostSaved,
    double Co2Saved,
    decimal SalePrice,
    decimal ResaleMargin,
    decimal ResaleServiceFee,
    decimal Co2Value,
    decimal NetValue,
    string Explanation,
    string MatchDetailsJson);

public record SaveRootCauseParams(
    string RootCause,
    string Frequency,
    string Recommendation,
    string Impact,
    double Confidence);

// ========================
// Result DTOs
// ========================

public record ReturnRequestDetailDto(
    Guid Id,
    Guid PackageId,
    string Reason,
    string Status,
    string? AiAnalysis,
    string? ResolutionNotes,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    string TrackingNumber,
    string SenderName,
    string RecipientName,
    string PackageStatus);

public record InventoryItemDto(
    Guid Id,
    Guid ReturnId,
    string ProductId,
    string Location,
    int HoldingDays,
    double MatchScore,
    string Status,
    string ProductName,
    string Category,
    string Condition,
    string Eligibility);

public record DemandHistoryDto(
    Guid Id,
    string ProductId,
    string Region,
    int OrderCount,
    double DemandScore,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record ReturnReasonByCategoryDto(
    string ReturnReason,
    string ProductName,
    string Location,
    int Count,
    double Percentage);
