namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Dashboard;

public interface IDashboardService
{
    Task<ApiResponse<DashboardMetricsDto>> GetMetricsAsync(DashboardFilterDto? filter, CancellationToken cancellationToken = default);
}
