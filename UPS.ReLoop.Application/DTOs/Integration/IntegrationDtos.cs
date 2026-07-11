namespace UPS.ReLoop.Application.DTOs.Integration;

using UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Complete return processing request that triggers the full agent pipeline.
/// </summary>
public record ReturnProcessingRequest
{
    public Guid PackageId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string ReturnReason { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string? ImageBase64 { get; init; }
    public string? AdditionalContext { get; init; }

    /// <summary>Holding days already completed (0-10) — drives the deterministic clock.</summary>
    public int? HoldingDaysCompleted { get; init; }

    /// <summary>Original pickup date; used when <see cref="HoldingDaysCompleted"/> is not supplied.</summary>
    public DateTime? PickupDate { get; init; }

    /// <summary>Base/list price used by the dynamic-pricing agent (defaults if omitted).</summary>
    public decimal? BasePrice { get; init; }
}

/// <summary>
/// Unified response from the complete agent pipeline.
/// </summary>
public class ReturnProcessingResponse
{
    public Guid ReturnRequestId { get; set; }
    public Guid PackageId { get; set; }
    public string Status { get; set; } = string.Empty;
    public ImageValidationResult? ImageValidation { get; set; }
    public MatchResult? HyperlocalMatch { get; set; }
    public RootCauseResult? RootCauseAnalysis { get; set; }
    public SavingsSummary Savings { get; set; } = new();

    // ReLoop Decision Engine outputs (differentiators).
    public HoldingClockResult? HoldingClock { get; set; }
    public PolicyComplianceResult? PolicyCompliance { get; set; }
    public DiversionDecision? Diversion { get; set; }
    public DecisionConfidence? DecisionConfidence { get; set; }
    public AutoApprovalResult? AutoApproval { get; set; }
    public RevenueOpportunity? RevenueOpportunity { get; set; }
    public List<Citation> Citations { get; set; } = [];

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class ImageValidationResult
{
    public string Condition { get; set; } = string.Empty;
    public int DamageScore { get; set; }
    public bool Eligible { get; set; }
    public double Confidence { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class MatchResult
{
    public int MatchScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class RootCauseResult
{
    public string RootCause { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
}

public class SavingsSummary
{
    public double DistanceSavedKm { get; set; }
    public decimal CostSaved { get; set; }
    public double Co2SavedKg { get; set; }
}
