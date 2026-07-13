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
                TotalReturns = metrics.TotalReturns,
                EligibleReturns = metrics.EligibleReturns,
                LocalMatches = metrics.LocalMatches,
                DiversionRate = metrics.DiversionRate,
                DistanceSavedKm = metrics.DistanceSavedKm,
                CostSaved = (decimal)metrics.CostSaved,
                Co2SavedKg = metrics.Co2SavedKg,
                TotalValueRecovered = metrics.TotalValueRecovered,
                ResaleMargin = metrics.ResaleMargin,
                ResaleServiceFee = metrics.ResaleServiceFee,
                Co2Value = metrics.Co2Value,
                AiCost = metrics.AiCost,
                RootCauseInsights = insights.Select(i => new RootCauseInsightDto
                {
                    Reason = i.Reason,
                    Count = i.Count,
                    Percentage = i.Percentage
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
        public int TotalReturns { get; set; }
        public int EligibleReturns { get; set; }
        public int LocalMatches { get; set; }
        public double DiversionRate { get; set; }
        public double DistanceSavedKm { get; set; }
        public double CostSaved { get; set; }
        public double Co2SavedKg { get; set; }
        public decimal TotalValueRecovered { get; set; }
        public decimal ResaleMargin { get; set; }
        public decimal ResaleServiceFee { get; set; }
        public decimal Co2Value { get; set; }
        public decimal AiCost { get; set; }
    }

    private class RootCauseInsightSpResult
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
