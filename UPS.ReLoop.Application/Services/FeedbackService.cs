namespace UPS.ReLoop.Application.Services;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.Common.Exceptions;
using UPS.ReLoop.Application.DTOs.Feedback;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Feedback service for the human-in-the-loop learning loop. For the MVP,
/// feedback is held in a thread-safe in-memory store (registered as a singleton);
/// in production this persists to a Feedback / AgentFeedback table and feeds the
/// demand + pricing models. The aggregate summary drives the accept-rate curve.
/// </summary>
public class FeedbackService : IFeedbackService
{
    private static readonly string[] ValidActions = ["Accept", "Modify", "Reject"];
    private readonly ConcurrentBag<StoredFeedback> _store;
    private readonly AutoApprovalMetrics _autoApprovalMetrics;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(ConcurrentBag<StoredFeedback> store, AutoApprovalMetrics autoApprovalMetrics, ILogger<FeedbackService> logger)
    {
        _store = store;
        _autoApprovalMetrics = autoApprovalMetrics;
        _logger = logger;
    }

    public Task<ApiResponse<AssociateFeedbackResponse>> CaptureAsync(AssociateFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (!ValidActions.Contains(request.Action, StringComparer.OrdinalIgnoreCase))
            throw new BadRequestException($"Action must be one of: {string.Join(", ", ValidActions)}.");

        var entry = new StoredFeedback
        {
            FeedbackId = Guid.NewGuid(),
            ReturnRequestId = request.ReturnRequestId,
            Action = Normalize(request.Action),
            CorrectedField = request.CorrectedField,
            OriginalValue = request.OriginalValue,
            CorrectedValue = request.CorrectedValue,
            AssociateId = request.AssociateId,
            Notes = request.Notes,
            CapturedAt = DateTime.UtcNow
        };

        _store.Add(entry);

        _logger.LogInformation("Feedback captured: {Action} for ReturnRequestId {Id} (field: {Field})",
            entry.Action, entry.ReturnRequestId, entry.CorrectedField ?? "-");

        var response = new AssociateFeedbackResponse
        {
            FeedbackId = entry.FeedbackId,
            ReturnRequestId = entry.ReturnRequestId,
            Action = entry.Action,
            CapturedAt = entry.CapturedAt,
            Message = entry.Action == "Modify"
                ? $"Correction on '{entry.CorrectedField}' captured — feeds the demand & pricing model."
                : $"{entry.Action} signal captured for the learning loop."
        };

        return Task.FromResult(ApiResponse<AssociateFeedbackResponse>.SuccessResponse(response, "Feedback recorded."));
    }

    public Task<ApiResponse<FeedbackSummary>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var all = _store.ToArray();
        var total = all.Length;
        var accepted = all.Count(f => f.Action == "Accept");
        var modified = all.Count(f => f.Action == "Modify");
        var rejected = all.Count(f => f.Action == "Reject");

        var topFields = all
            .Where(f => !string.IsNullOrWhiteSpace(f.CorrectedField))
            .GroupBy(f => f.CorrectedField!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FieldCorrectionStat(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .Take(5)
            .ToList();

        var summary = new FeedbackSummary
        {
            Total = total,
            Accepted = accepted,
            Modified = modified,
            Rejected = rejected,
            AcceptRate = total == 0 ? 0 : Math.Round((double)accepted / total * 100, 1),
            TopCorrectedFields = topFields,
            AutoApproval = _autoApprovalMetrics.Snapshot()
        };

        return Task.FromResult(ApiResponse<FeedbackSummary>.SuccessResponse(summary, "Feedback summary."));
    }

    private static string Normalize(string action) =>
        char.ToUpperInvariant(action[0]) + action[1..].ToLowerInvariant();

    /// <summary>
    /// Builds the singleton store pre-seeded with synthetic associate reviews so the
    /// human-in-the-loop accept-rate curve is populated on a fresh start. In production
    /// this is replaced by the persisted AgentFeedback table.
    /// </summary>
    public static ConcurrentBag<StoredFeedback> CreateSeededStore()
    {
        var bag = new ConcurrentBag<StoredFeedback>();
        string[] fields = ["Resale Price", "Condition Grade", "Category", "Destination Channel"];
        for (var i = 0; i < 480; i++)
        {
            var roll = i % 100;
            string action;
            string? field = null;
            if (roll < 87) action = "Accept";
            else if (roll < 96) { action = "Modify"; field = fields[i % fields.Length]; }
            else action = "Reject";

            bag.Add(new StoredFeedback
            {
                FeedbackId = Guid.NewGuid(),
                ReturnRequestId = Guid.NewGuid(),
                Action = action,
                CorrectedField = field,
                AssociateId = $"assoc-{(i % 12) + 1:00}",
                CapturedAt = DateTime.UtcNow.AddHours(-i)
            });
        }
        return bag;
    }

    /// <summary>In-memory feedback record (singleton-scoped for the MVP).</summary>
    public class StoredFeedback
    {
        public Guid FeedbackId { get; set; }
        public Guid ReturnRequestId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? CorrectedField { get; set; }
        public string? OriginalValue { get; set; }
        public string? CorrectedValue { get; set; }
        public string? AssociateId { get; set; }
        public string? Notes { get; set; }
        public DateTime CapturedAt { get; set; }
    }
}
