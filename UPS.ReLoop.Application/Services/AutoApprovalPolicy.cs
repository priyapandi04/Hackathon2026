namespace UPS.ReLoop.Application.Services;

using UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Straight-through-processing (STP) router. Answers the operational question:
/// "with thousands of returns a day, you cannot manually accept/modify/reject
/// every one — which ones can we safely auto-approve, and which must a human see?"
///
/// The policy is a deterministic tiered gate:
///   1. Certain deterministic actions (policy-restricted, expired clock) auto-commit.
///   2. High confidence + policy-clean + low dollar value    -> AUTO_APPROVE.
///   3. Medium confidence OR high dollar value                -> HUMAN_REVIEW.
///   4. Below the escalation threshold (or already escalated) -> ESCALATE.
/// A small random slice of AUTO_APPROVE items is still flagged for a post-hoc QA
/// audit so throughput is not blocked yet quality stays measurable.
///
/// Thresholds are illustrative starting points — tune them against real
/// accept/override rates from the feedback loop.
/// </summary>
public static class AutoApprovalPolicy
{
    /// <summary>At/above this confidence a policy-clean, low-value item auto-approves.</summary>
    public const double AutoApproveConfidenceThreshold = 0.85;

    /// <summary>Items worth more than this always get a human sign-off (dollar-risk guardrail).</summary>
    public const decimal HighValueReviewThreshold = 150m;

    /// <summary>Fraction of auto-approved items flagged for a post-hoc QA audit.</summary>
    public const double QaSampleRate = 0.02;

    public const string RouteAutoApprove = "AUTO_APPROVE";
    public const string RouteHumanReview = "HUMAN_REVIEW";
    public const string RouteEscalate = "ESCALATE";

    /// <summary>
    /// Decide the review route for a processed return.
    /// </summary>
    /// <param name="confidence">Calibrated decision confidence for the item.</param>
    /// <param name="itemValue">Base/list value of the item (dollar-risk guardrail).</param>
    /// <param name="policyRestricted">True when a retailer policy blocks resale (certain action).</param>
    /// <param name="clockExpired">True when the 10-day holding window has elapsed (certain action).</param>
    /// <param name="stableKey">
    /// A stable identifier (e.g. return-request id) used to deterministically pick the
    /// QA audit sample — same item always yields the same sampling decision.
    /// </param>
    public static AutoApprovalResult Evaluate(
        DecisionConfidence confidence,
        decimal itemValue,
        bool policyRestricted,
        bool clockExpired,
        string? stableKey = null)
    {
        var result = new AutoApprovalResult
        {
            ConfidenceScore = confidence.Score,
            ItemValue = itemValue
        };

        // 1. Deterministic, certain outcomes commit without a human. A policy block
        //    or an expired clock is a rule, not a judgement call.
        if (policyRestricted || clockExpired)
        {
            result.Route = RouteAutoApprove;
            result.RequiresHumanReview = false;
            result.Reason = policyRestricted
                ? "Deterministic compliance action (retailer policy block) — auto-committed, no human needed."
                : "Deterministic compliance action (10-day holding window elapsed) — auto-committed, no human needed.";
            return result;
        }

        // 2. Low confidence (or an already-escalated decision) goes to the exception queue.
        if (confidence.ShouldEscalate)
        {
            result.Route = RouteEscalate;
            result.RequiresHumanReview = true;
            result.Reason = $"Confidence {confidence.Score:0.00} below escalation threshold — routed to supervisor exception queue.";
            return result;
        }

        // 3. Dollar-risk guardrail: expensive items always get a human, even when confident.
        if (itemValue > HighValueReviewThreshold)
        {
            result.Route = RouteHumanReview;
            result.RequiresHumanReview = true;
            result.Reason = $"Item value ${itemValue:0.##} exceeds the ${HighValueReviewThreshold:0.##} auto-approval limit — human sign-off required.";
            return result;
        }

        // 4. Confident, policy-clean, low-value -> straight-through auto-approval.
        if (confidence.Score >= AutoApproveConfidenceThreshold)
        {
            var sampled = IsSampledForQa(stableKey);
            result.Route = RouteAutoApprove;
            result.RequiresHumanReview = false;
            result.SampledForQaAudit = sampled;
            result.Reason = sampled
                ? $"High confidence {confidence.Score:0.00} — auto-approved (flagged for QA audit sample)."
                : $"High confidence {confidence.Score:0.00}, policy-clean, low value — auto-approved.";
            return result;
        }

        // 5. Everything in the medium band is reviewed by an associate.
        result.Route = RouteHumanReview;
        result.RequiresHumanReview = true;
        result.Reason = $"Confidence {confidence.Score:0.00} in the review band [{DecisionConfidenceEvaluator.EscalationThreshold:0.00}, {AutoApproveConfidenceThreshold:0.00}) — associate review.";
        return result;
    }

    /// <summary>
    /// Deterministically decide whether an item falls into the QA audit sample.
    /// Uses a stable hash of the key so the same item is always sampled the same
    /// way (reproducible), without a random-number generator.
    /// </summary>
    private static bool IsSampledForQa(string? stableKey)
    {
        if (string.IsNullOrEmpty(stableKey) || QaSampleRate <= 0)
            return false;

        // Stable, non-cryptographic bucket in [0,1).
        uint hash = 2166136261u;
        foreach (var c in stableKey)
        {
            hash ^= c;
            hash *= 16777619u;
        }

        var bucket = (hash % 10000) / 10000.0;
        return bucket < QaSampleRate;
    }
}
