namespace UPS.ReLoop.Application.DTOs.Dashboard;

/// <summary>
/// Main dashboard response containing all KPI metrics.
/// </summary>
public record DashboardMetricsDto
{
    public int TotalReturns { get; init; }
    public int EligibleReturns { get; init; }
    public int LocalMatches { get; init; }
    public double DiversionRate { get; init; }
    public double DistanceSavedKm { get; init; }
    public decimal CostSaved { get; init; }
    public double Co2SavedKg { get; init; }
    public List<RootCauseInsightDto> RootCauseInsights { get; init; } = [];
}

/// <summary>
/// Root cause breakdown for return reasons.
/// </summary>
public record RootCauseInsightDto
{
    public string Reason { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// Optional filter for dashboard queries.
/// </summary>
public record DashboardFilterDto
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary>
/// One data point in the daily savings/diversion trend line (30-day window).
/// </summary>
public record DashboardTrendPointDto
{
    public string Date { get; init; } = string.Empty;
    public int Returns { get; init; }
    public int LocalMatches { get; init; }
    public double CostSaved { get; init; }
    public double DistanceSavedKm { get; init; }
    public double Co2SavedKg { get; init; }
}

/// <summary>
/// Performance metrics for a single AI agent.
/// Sourced from MatchAgentResults, AgentRecommendations (via SP) and
/// AutoApprovalMetrics in-memory singleton (merged in service layer).
/// </summary>
public record AgentTelemetryDto
{
    public string AgentName { get; init; } = string.Empty;
    public int TotalRuns { get; init; }
    public int SuccessfulRuns { get; init; }
    public double PrecisionRate { get; init; }
    public double EscalationRate { get; init; }
    public int AverageResponseTime { get; init; }
}

/// <summary>
/// Per-segment (category) analytics for the retailer portal.
/// </summary>
public record SegmentAnalyticsDto
{
    public string Segment { get; init; } = string.Empty;
    public int TotalReturns { get; init; }
    public int ItemsResold { get; init; }
    public double DiversionRate { get; init; }
    public double RevenueRecovered { get; init; }
    public double Co2SavedKg { get; init; }
    public double DistanceSavedKm { get; init; }
    public double AvgMatchScore { get; init; }
    public double AvgConfidence { get; init; }
    public double AvgDaysToSell { get; init; }
    public List<SegmentReasonDto> TopReasons { get; init; } = [];
    public List<SegmentTrendPointDto> Trend { get; init; } = [];
}

public record SegmentReasonDto
{
    public string Reason { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Share { get; init; }
    public string TopLocation { get; init; } = string.Empty;
    public double EstimatedAnnualImpact { get; init; }
}

public record SegmentTrendPointDto
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

/// <summary>
/// Per-location (hub/city) analytics for the dashboard map and leaderboard.
/// </summary>
public record LocationAnalyticsDto
{
    public string Location { get; init; } = string.Empty;
    public int Returns { get; init; }
    public double CostRecovered { get; init; }
    public double Co2SavedKg { get; init; }
    public double AvgMatchScore { get; init; }
}
