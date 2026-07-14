namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Dashboard;

public interface IDashboardService
{
    Task<ApiResponse<DashboardMetricsDto>> GetMetricsAsync(DashboardFilterDto? filter, CancellationToken cancellationToken = default);

    /// <summary>Per-segment (product-category) analytics for the partner portal, aggregated from live data.</summary>
    Task<ApiResponse<List<SegmentAnalyticsDto>>> GetSegmentAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>Per-location analytics (volume + INR recovered) for the executive charts.</summary>
    Task<ApiResponse<List<LocationAnalyticsDto>>> GetLocationAnalyticsAsync(CancellationToken cancellationToken = default);

    Task<ApiResponse<List<DashboardTrendPointDto>>> GetTrendAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AgentTelemetryDto>>> GetAgentTelemetryAsync(CancellationToken cancellationToken = default);
}
