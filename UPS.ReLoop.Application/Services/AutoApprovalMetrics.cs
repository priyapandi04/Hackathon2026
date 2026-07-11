namespace UPS.ReLoop.Application.Services;

using System.Collections.Concurrent;
using UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Thread-safe in-memory tally of straight-through-processing (STP) routing
/// outcomes produced by <see cref="AutoApprovalPolicy"/>. Registered as a
/// singleton for the MVP so the dashboard can show the auto-vs-manual split;
/// in production this is persisted alongside the decision log.
/// </summary>
public sealed class AutoApprovalMetrics
{
    private readonly ConcurrentDictionary<string, int> _routeCounts = new(StringComparer.OrdinalIgnoreCase);
    private int _qaSampled;

    public void Record(AutoApprovalResult result)
    {
        if (result is null)
            return;

        _routeCounts.AddOrUpdate(result.Route, 1, (_, current) => current + 1);
        if (result.SampledForQaAudit)
            Interlocked.Increment(ref _qaSampled);
    }

    public AutoApprovalStats Snapshot()
    {
        var autoApproved = _routeCounts.GetValueOrDefault(AutoApprovalPolicy.RouteAutoApprove);
        var humanReview = _routeCounts.GetValueOrDefault(AutoApprovalPolicy.RouteHumanReview);
        var escalated = _routeCounts.GetValueOrDefault(AutoApprovalPolicy.RouteEscalate);
        var total = autoApproved + humanReview + escalated;

        return new AutoApprovalStats
        {
            Total = total,
            AutoApproved = autoApproved,
            HumanReview = humanReview,
            Escalated = escalated,
            QaSampled = Volatile.Read(ref _qaSampled),
            StpRate = total == 0 ? 0 : Math.Round((double)autoApproved / total * 100, 1)
        };
    }
}
