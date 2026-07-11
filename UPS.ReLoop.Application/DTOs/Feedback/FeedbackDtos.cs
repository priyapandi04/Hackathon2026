namespace UPS.ReLoop.Application.DTOs.Feedback;

/// <summary>
/// Human-in-the-loop decision captured from the store associate console.
/// Accept = positive signal, Reject = negative, Modify = the richest signal
/// (captures exactly which field the associate corrected). Feeds the daily
/// learning loop and the diversion / accept-rate curves.
/// </summary>
public record AssociateFeedbackRequest
{
    public Guid ReturnRequestId { get; init; }

    /// <summary>Accept | Modify | Reject</summary>
    public string Action { get; init; } = "Accept";

    /// <summary>Which field was corrected on Modify (e.g. price, condition, eligibility).</summary>
    public string? CorrectedField { get; init; }
    public string? OriginalValue { get; init; }
    public string? CorrectedValue { get; init; }
    public string? AssociateId { get; init; }
    public string? Notes { get; init; }
}

public class AssociateFeedbackResponse
{
    public Guid FeedbackId { get; set; }
    public Guid ReturnRequestId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Aggregated learning signal used by dashboards to prove ROI over time.</summary>
public class FeedbackSummary
{
    public int Total { get; set; }
    public int Accepted { get; set; }
    public int Modified { get; set; }
    public int Rejected { get; set; }
    public double AcceptRate { get; set; }
    public List<FieldCorrectionStat> TopCorrectedFields { get; set; } = [];
}

public record FieldCorrectionStat(string Field, int Count);
