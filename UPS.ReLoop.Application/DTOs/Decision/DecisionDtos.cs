namespace UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Result of the deterministic 10-day holding clock. This is the core UPS
/// problem-statement constraint: items are held locally for up to 10 days; if
/// unmatched by day 10 they are automatically returned to the seller.
/// All values are computed in code (never by the LLM) so they are auditable.
/// </summary>
public class HoldingClockResult
{
    public const int MaxHoldingDays = 10;

    public int HoldingDay { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpired { get; set; }
    public bool AutoReturnTriggered { get; set; }

    /// <summary>OnTrack | ClosingWindow | Expired</summary>
    public string ClockStatus { get; set; } = "OnTrack";
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Policy-first compliance decision. A retailer-policy block always overrides
/// physical condition (e.g. an opened hygiene item is refused even if pristine).
/// Every decision carries a citation (<see cref="PolicyRef"/>) so the reasoning
/// is grounded and auditable rather than an LLM opinion.
/// </summary>
public class PolicyComplianceResult
{
    public bool ResaleAllowed { get; set; }
    public bool IsRestrictedCategory { get; set; }
    public string PolicyRef { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    /// <summary>Cosine similarity of the retrieved governing policy (0-1). 0 when no RAG match cleared the threshold.</summary>
    public double RetrievalScore { get; set; }

    /// <summary>The policy text snippet the decision was grounded on (RAG evidence).</summary>
    public string RetrievedSnippet { get; set; } = string.Empty;
}

/// <summary>
/// Output of the autonomous Diversion / Dynamic-Pricing agent. As the 10-day
/// clock advances without a local match it lowers price, widens the search
/// radius, offers to nearby Access Points, and finally escalates / returns.
/// </summary>
public class DiversionDecision
{
    /// <summary>SELL_LOCAL | HOLD | WIDEN_RADIUS | DISCOUNT_SELL | OFFER_ACCESS_POINTS | RETURN_TO_SELLER | ESCALATE</summary>
    public string Action { get; set; } = "HOLD";
    public decimal BasePrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public double PriceAdjustmentPct { get; set; }
    public double SearchRadiusKm { get; set; }
    public bool Escalated { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// A single grounded evidence reference (policy clause or historical precedent)
/// backing a decision. Enables anti-hallucination: every claim cites a real ref.
/// </summary>
public record Citation(string SourceType, string RefId, string Snippet);

/// <summary>
/// Calibrated confidence for the whole decision. Below the escalation threshold
/// the pipeline refuses to auto-decide and routes to a human ("knows when it
/// doesn't know").
/// </summary>
public class DecisionConfidence
{
    public double Score { get; set; }
    /// <summary>High | Medium | Low</summary>
    public string Band { get; set; } = "Medium";
    public bool ShouldEscalate { get; set; }
    public List<string> Factors { get; set; } = [];
}

/// <summary>
/// Triple-value capture plus new-revenue economics for one diverted item:
/// cost avoided (reverse freight) + revenue earned (resale margin, resale-as-a-
/// service fee) + quantified CO2 value.
/// </summary>
public class RevenueOpportunity
{
    public decimal FreightAvoided { get; set; }
    public decimal ResaleMargin { get; set; }
    public decimal ResaleServiceFee { get; set; }
    public decimal Co2ValueInr { get; set; }
    public decimal AiCost { get; set; }
    public decimal TotalNetValue { get; set; }
}

/// <summary>
/// Straight-through-processing (STP) routing decision. At scale (thousands of
/// returns per day) a human cannot accept/modify/reject every item, so the
/// engine auto-approves the confident, low-risk, policy-clean tail and reserves
/// human attention for the uncertain or high-value minority. Fully deterministic
/// and auditable so an auto-approval can always be explained after the fact.
/// </summary>
public class AutoApprovalResult
{
    /// <summary>AUTO_APPROVE | HUMAN_REVIEW | ESCALATE</summary>
    public string Route { get; set; } = "HUMAN_REVIEW";

    /// <summary>True when a human associate must accept/modify/reject before the action commits.</summary>
    public bool RequiresHumanReview { get; set; }

    /// <summary>Confidence score that drove the routing (0-1).</summary>
    public double ConfidenceScore { get; set; }

    /// <summary>Item value considered for the dollar-risk guardrail.</summary>
    public decimal ItemValue { get; set; }

    /// <summary>
    /// True when an AUTO_APPROVE item was randomly picked for a post-hoc QA audit.
    /// It still auto-commits, but is flagged so quality can be sampled without
    /// blocking throughput.
    /// </summary>
    public bool SampledForQaAudit { get; set; }

    public string Reason { get; set; } = string.Empty;
}

/// <summary>Aggregated STP routing counts for the operations dashboard.</summary>
public class AutoApprovalStats
{
    public int Total { get; set; }
    public int AutoApproved { get; set; }
    public int HumanReview { get; set; }
    public int Escalated { get; set; }
    public int QaSampled { get; set; }

    /// <summary>Percentage of routed items auto-approved without human touch.</summary>
    public double StpRate { get; set; }
}
