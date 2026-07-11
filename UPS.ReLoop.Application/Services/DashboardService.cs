namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Dashboard;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class DashboardService : IDashboardService
{
    private readonly IDashboardSpRepository _dashboardSpRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardService> _logger;
    private const string CacheKeyPrefix = "dashboard_metrics";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public DashboardService(IDashboardSpRepository dashboardSpRepo, IMemoryCache cache, ILogger<DashboardService> logger)
    {
        _dashboardSpRepo = dashboardSpRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResponse<DashboardMetricsDto>> GetMetricsAsync(DashboardFilterDto? filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}_{filter?.FromDate}_{filter?.ToDate}";

        if (_cache.TryGetValue(cacheKey, out DashboardMetricsDto? cached) && cached is not null)
        {
            _logger.LogInformation("Dashboard metrics returned from cache for key: {CacheKey}", cacheKey);
            return ApiResponse<DashboardMetricsDto>.SuccessResponse(cached, "Dashboard metrics retrieved from cache.");
        }

        _logger.LogInformation("Cache miss for key: {CacheKey}, executing stored procedures", cacheKey);

        var metrics = await _dashboardSpRepo.GetMetricsAsync(filter?.FromDate, filter?.ToDate, cancellationToken);

        _cache.Set(cacheKey, metrics, CacheDuration);

        _logger.LogInformation("Dashboard metrics cached for {Duration} minutes. TotalReturns: {Total}, LocalMatches: {Matches}",
            CacheDuration.TotalMinutes, metrics.TotalReturns, metrics.LocalMatches);

        return ApiResponse<DashboardMetricsDto>.SuccessResponse(metrics, "Dashboard metrics retrieved successfully.");
    }
}
