namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Dashboard;

public interface IDashboardService
{
    Task<ApiResponse<DashboardMetricsDto>> GetMetricsAsync(DashboardFilterDto? filter, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<DashboardTrendPointDto>>> GetTrendAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AgentTelemetryDto>>> GetAgentTelemetryAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<List<SegmentAnalyticsDto>>> GetSegmentsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<List<LocationAnalyticsDto>>> GetLocationsAsync(CancellationToken cancellationToken = default);
}
