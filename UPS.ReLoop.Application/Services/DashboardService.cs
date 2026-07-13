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
    private readonly ISegmentAnalyticsRepository _segmentRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardService> _logger;
    private const string CacheKeyPrefix = "dashboard_metrics";
    private const string SegmentCacheKey = "dashboard_segments";
    private const string LocationCacheKey = "dashboard_locations";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public DashboardService(
        IDashboardSpRepository dashboardSpRepo,
        ISegmentAnalyticsRepository segmentRepo,
        IMemoryCache cache,
        ILogger<DashboardService> logger)
    {
        _dashboardSpRepo = dashboardSpRepo;
        _segmentRepo = segmentRepo;
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

    public async Task<ApiResponse<List<SegmentAnalyticsDto>>> GetSegmentAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(SegmentCacheKey, out List<SegmentAnalyticsDto>? cached) && cached is not null)
        {
            return ApiResponse<List<SegmentAnalyticsDto>>.SuccessResponse(cached, "Segment analytics retrieved from cache.");
        }

        var segments = await _segmentRepo.GetSegmentAnalyticsAsync(cancellationToken);
        _cache.Set(SegmentCacheKey, segments, CacheDuration);

        _logger.LogInformation("Segment analytics computed for {Count} segments.", segments.Count);
        return ApiResponse<List<SegmentAnalyticsDto>>.SuccessResponse(segments, "Segment analytics retrieved successfully.");
    }

    public async Task<ApiResponse<List<LocationAnalyticsDto>>> GetLocationAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(LocationCacheKey, out List<LocationAnalyticsDto>? cached) && cached is not null)
        {
            return ApiResponse<List<LocationAnalyticsDto>>.SuccessResponse(cached, "Location analytics retrieved from cache.");
        }

        var locations = await _segmentRepo.GetLocationAnalyticsAsync(cancellationToken);
        _cache.Set(LocationCacheKey, locations, CacheDuration);

        _logger.LogInformation("Location analytics computed for {Count} locations.", locations.Count);
        return ApiResponse<List<LocationAnalyticsDto>>.SuccessResponse(locations, "Location analytics retrieved successfully.");
    }
}
