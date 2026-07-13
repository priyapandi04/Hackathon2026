namespace UPS.ReLoop.Infrastructure.Persistence.Repositories;

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UPS.ReLoop.Application.DTOs.Dashboard;
using UPS.ReLoop.Application.Interfaces.Repositories;
using UPS.ReLoop.Application.Services;

/// <summary>
/// Builds per-segment (product-category) analytics entirely from live data:
/// MatchAgentResults for economics + ReturnRequests for the root-cause reasons.
/// No hardcoded partner figures — everything is aggregated on demand.
/// </summary>
public class SegmentAnalyticsRepository : ISegmentAnalyticsRepository
{
    private readonly ApplicationDbContext _context;

    public SegmentAnalyticsRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<SegmentAnalyticsDto>> GetSegmentAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        // Pull the raw match rows once (a few thousand) and aggregate in memory —
        // avoids a bespoke SP and keeps the logic beside the C# decision engine.
        var matches = await _context.MatchAgentResults
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m => !m.IsDeleted)
            .Select(m => new
            {
                m.ReturnRequestId,
                m.Category,
                m.Location,
                m.MatchScore,
                m.Recommendation,
                m.Confidence,
                m.CostSaved,
                m.Co2Saved,
                m.DistanceSavedKm,
                m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (matches.Count == 0) return [];

        // Reason lookup for the matched returns (root-cause tickets).
        var returnIds = matches.Select(m => m.ReturnRequestId).Distinct().ToList();
        var reasons = await _context.ReturnRequests
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => returnIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Reason })
            .ToListAsync(cancellationToken);
        var reasonById = reasons
            .GroupBy(r => r.Id)
            .ToDictionary(g => g.Key, g => g.First().Reason);

        var result = new List<SegmentAnalyticsDto>();

        foreach (var group in matches.GroupBy(m => string.IsNullOrWhiteSpace(m.Category) ? "General" : m.Category))
        {
            var rows = group.ToList();
            var total = rows.Count;
            var resold = rows.Count(r =>
                r.MatchScore >= 60 ||
                r.Recommendation.Contains("SELL_LOCAL", StringComparison.OrdinalIgnoreCase) ||
                r.Recommendation.Contains("REDISTRIBUTE", StringComparison.OrdinalIgnoreCase));
            var revenue = (decimal)rows.Sum(r => r.CostSaved);

            // Top return reasons within the segment → priced fix tickets.
            var topReasons = rows
                .Select(r => new
                {
                    Reason = reasonById.TryGetValue(r.ReturnRequestId, out var rs) && !string.IsNullOrWhiteSpace(rs)
                        ? rs : "Unspecified",
                    r.Location,
                    r.CostSaved
                })
                .GroupBy(x => x.Reason)
                .Select(g => new SegmentReasonDto
                {
                    Reason = g.Key,
                    Count = g.Count(),
                    Share = Math.Round((double)g.Count() / total * 100, 1),
                    TopLocation = g.GroupBy(x => x.Location)
                        .OrderByDescending(l => l.Count())
                        .Select(l => l.Key)
                        .FirstOrDefault() ?? "—",
                    // Revenue tied to this reason, projected to an annual run-rate (×12).
                    EstimatedAnnualImpact = Math.Round((decimal)g.Sum(x => x.CostSaved) * 12m, 0)
                })
                .OrderByDescending(r => r.Count)
                .Take(4)
                .ToList();

            // Last 6 calendar months of return volume for the trend line.
            var trend = rows
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new SegmentTrendPointDto
                {
                    Label = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month),
                    Count = g.Count()
                })
                .TakeLast(6)
                .ToList();

            result.Add(new SegmentAnalyticsDto
            {
                Segment = group.Key,
                TotalReturns = total,
                ItemsResold = resold,
                DiversionRate = Math.Round((double)resold / total * 100, 1),
                RevenueRecovered = Math.Round(revenue, 0),
                Co2SavedKg = Math.Round(rows.Sum(r => r.Co2Saved), 1),
                DistanceSavedKm = Math.Round(rows.Sum(r => r.DistanceSavedKm), 0),
                AvgMatchScore = Math.Round(rows.Average(r => r.MatchScore), 1),
                AvgConfidence = Math.Round(rows.Average(r => r.Confidence), 2),
                // Days-to-sell reuses the Match Agent's deterministic estimator so the
                // dashboard figure is identical to what the per-item pipeline reports.
                AvgDaysToSell = Math.Round(rows.Average(r => MatchCalculator.EstimateDaysToSell(r.MatchScore)), 1),
                TopReasons = topReasons,
                Trend = trend
            });
        }

        return result.OrderByDescending(s => s.RevenueRecovered).ToList();
    }

    /// <summary>Per-location aggregates for the executive charts (volume vs value).</summary>
    public async Task<List<LocationAnalyticsDto>> GetLocationAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.MatchAgentResults
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m => !m.IsDeleted)
            .Select(m => new { m.Location, m.CostSaved, m.Co2Saved, m.MatchScore })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return [];

        return rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Location) ? "Unknown" : r.Location)
            .Select(g => new LocationAnalyticsDto
            {
                Location = g.Key,
                Returns = g.Count(),
                CostRecovered = Math.Round((decimal)g.Sum(r => r.CostSaved), 0),
                Co2SavedKg = Math.Round(g.Sum(r => r.Co2Saved), 1),
                AvgMatchScore = Math.Round(g.Average(r => r.MatchScore), 1)
            })
            .OrderByDescending(l => l.Returns)
            .ToList();
    }
}
