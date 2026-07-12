namespace UPS.ReLoop.Application.Services;

using UPS.ReLoop.Application.DTOs.Decision;

/// <summary>
/// Calibrated-trust gate. Combines the signals available in the pipeline
/// (image-condition certainty, local-match strength, policy clarity) into a
/// single decision confidence and decides whether to escalate to a human.
/// Deterministic — this is the "knows when it doesn't know" guardrail.
/// </summary>
public static class DecisionConfidenceEvaluator
{
    /// <summary>Below this score the pipeline refuses to auto-decide and escalates.</summary>
    public const double EscalationThreshold = 0.60;

    public static DecisionConfidence Evaluate(
        double? imageConfidence,
        double matchConfidence,
        bool policyResolved,
        bool policyRestricted)
    {
        var factors = new List<string>();

        // Match, image and policy signals are all on a 0-1 scale.
        var match01 = Math.Clamp(matchConfidence, 0, 1);
        var image01 = imageConfidence is { } ic ? Math.Clamp(ic, 0, 1) : 0.5; // neutral if no image
        var policy01 = policyResolved ? 1.0 : 0.4;

        if (imageConfidence is null) factors.Add("No image provided — condition certainty reduced.");
        if (!policyResolved) factors.Add("No explicit retailer policy matched — grounding weak.");

        // A clean policy block is a HIGH-confidence outcome (we are certain we must refuse).
        if (policyRestricted)
        {
            factors.Add("Policy-restricted category — high-confidence return-to-seller.");
            return new DecisionConfidence
            {
                Score = 0.95,
                Band = "High",
                ShouldEscalate = false,
                Factors = factors
            };
        }

        // Weighted blend: match strength dominates, then image, then policy clarity.
        var score = Math.Round((0.5 * match01) + (0.3 * image01) + (0.2 * policy01), 2);

        var band = score >= 0.8 ? "High" : score >= EscalationThreshold ? "Medium" : "Low";
        var escalate = score < EscalationThreshold;
        if (escalate) factors.Add($"Confidence {score:0.00} below escalation threshold {EscalationThreshold:0.00}.");

        return new DecisionConfidence
        {
            Score = score,
            Band = band,
            ShouldEscalate = escalate,
            Factors = factors
        };
    }
}
