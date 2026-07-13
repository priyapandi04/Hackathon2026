namespace UPS.ReLoop.Infrastructure.Persistence.Repositories.StoredProcedures;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.DTOs.Dashboard;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class DashboardSpRepository : IDashboardSpRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardSpRepository> _logger;

    public DashboardSpRepository(ApplicationDbContext context, ILogger<DashboardSpRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_GetDashboardMetrics FromDate: {FromDate}, ToDate: {ToDate}", fromDate, toDate);

        try
        {
            // Result set 1: KPI metrics
            var metricsParams = new[]
            {
                new SqlParameter("@FromDate", SqlDbType.DateTime2) { Value = (object?)fromDate ?? DBNull.Value },
                new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = (object?)toDate ?? DBNull.Value }
            };

            var metricsResults = await _context.Database
                .SqlQueryRaw<DashboardMetricsSpResult>(
                    "EXEC [dbo].[usp_GetDashboardMetrics] @FromDate, @ToDate",
                    metricsParams)
                .ToListAsync(cancellationToken);

            var metrics = metricsResults.FirstOrDefault();

            if (metrics is null)
            {
                return new DashboardMetricsDto();
            }

            // Result set 2: Root cause insights (separate SP call)
            var insightParams = new[]
            {
                new SqlParameter("@FromDate", SqlDbType.DateTime2) { Value = (object?)fromDate ?? DBNull.Value },
                new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = (object?)toDate ?? DBNull.Value }
            };

            var insights = await _context.Database
                .SqlQueryRaw<RootCauseInsightSpResult>(
                    "EXEC [dbo].[usp_GetDashboardRootCauseInsights] @FromDate, @ToDate",
                    insightParams)
                .ToListAsync(cancellationToken);

            return new DashboardMetricsDto
            {
                TotalReturns = metrics.TotalReturns ?? 0,
                EligibleReturns = metrics.EligibleReturns ?? 0,
                LocalMatches = metrics.LocalMatches ?? 0,
                DiversionRate = metrics.DiversionRate ?? 0,
                DistanceSavedKm = metrics.DistanceSavedKm ?? 0,
                CostSaved = (decimal)(metrics.CostSaved ?? 0),
                Co2SavedKg = metrics.Co2SavedKg ?? 0,
                RootCauseInsights = insights.Select(i => new RootCauseInsightDto
                {
                    Reason = i.Reason ?? string.Empty,
                    Count = i.Count ?? 0,
                    Percentage = i.Percentage ?? 0
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing usp_GetDashboardMetrics");
            throw;
        }
    }

    private class DashboardMetricsSpResult
    {
        public int? TotalReturns { get; set; }
        public int? EligibleReturns { get; set; }
        public int? LocalMatches { get; set; }
        public double? DiversionRate { get; set; }
        public double? DistanceSavedKm { get; set; }
        public double? CostSaved { get; set; }
        public double? Co2SavedKg { get; set; }
    }

    private class RootCauseInsightSpResult
    {
        public string? Reason { get; set; }
        public int? Count { get; set; }
        public double? Percentage { get; set; }
    }

    // ---- Trend ----

    public async Task<IReadOnlyList<DashboardTrendPointDto>> GetTrendAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_GetDashboardTrend Days: {Days}", days);

        var parameters = new[]
        {
            new SqlParameter("@Days", SqlDbType.Int) { Value = days }
        };

        var results = await _context.Database
            .SqlQueryRaw<TrendSpResult>(
                "EXEC [dbo].[usp_GetDashboardTrend] @Days",
                parameters)
            .ToListAsync(cancellationToken);

        return results.Select(r => new DashboardTrendPointDto
        {
            Date = r.Date.ToString("yyyy-MM-dd"),
            Returns = r.Returns,
            LocalMatches = r.LocalMatches,
            CostSaved = r.CostSaved,
            DistanceSavedKm = r.DistanceSavedKm,
            Co2SavedKg = r.Co2SavedKg,
        }).ToList().AsReadOnly();
    }

    private class TrendSpResult
    {
        public DateTime Date { get; set; }
        public int Returns { get; set; }
        public int LocalMatches { get; set; }
        public double CostSaved { get; set; }
        public double DistanceSavedKm { get; set; }
        public double Co2SavedKg { get; set; }
    }

    // ---- Agent Telemetry ----

    public async Task<IReadOnlyList<AgentTelemetryDto>> GetAgentTelemetryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing usp_GetAgentTelemetry");

        var results = await _context.Database
            .SqlQueryRaw<AgentTelemetrySpResult>("EXEC [dbo].[usp_GetAgentTelemetry]")
            .ToListAsync(cancellationToken);

        return results.Select(r => new AgentTelemetryDto
        {
            AgentName = r.AgentName,
            DecisionsMade = r.DecisionsMade,
            Precision = r.Precision,
            EscalationRate = r.EscalationRate,
        }).ToList().AsReadOnly();
    }

    private class AgentTelemetrySpResult
    {
        public string AgentName { get; set; } = string.Empty;
        public int DecisionsMade { get; set; }
        public double Precision { get; set; }
        public double EscalationRate { get; set; }
    }
}
