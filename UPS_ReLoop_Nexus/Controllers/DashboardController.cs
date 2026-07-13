namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.DTOs.Dashboard;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Dashboard API providing KPI metrics for UPS ReLoop Nexus.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Retrieves all dashboard KPI metrics including total returns, eligible returns,
    /// local matches, diversion rate, distance saved, cost saved, CO2 saved, and root cause insights.
    /// </summary>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard metrics response.</returns>
    /// <response code="200">Returns the dashboard metrics.</response>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(Application.Common.ApiResponse<DashboardMetricsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var filter = new DashboardFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _dashboardService.GetMetricsAsync(filter, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Per-segment (product-category) analytics for the partner portal — real aggregates
    /// (returns, resale rate, recovered revenue in INR, CO2, top root-cause reasons, trend).
    /// </summary>
    /// <response code="200">Returns live segment analytics.</response>
    [HttpGet("segments")]
    [ProducesResponseType(typeof(Application.Common.ApiResponse<List<SegmentAnalyticsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSegments(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSegmentAnalyticsAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Per-location analytics for the executive charts — returns volume by region and
    /// INR value recovered by location (two distinct views), aggregated live.
    /// </summary>
    /// <response code="200">Returns live location analytics.</response>
    [HttpGet("locations")]
    [ProducesResponseType(typeof(Application.Common.ApiResponse<List<LocationAnalyticsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocations(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetLocationAnalyticsAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
