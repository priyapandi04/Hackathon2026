namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.DTOs.Decision;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Policy-first eligibility grounding via retrieval-augmented generation (RAG).
/// A query (category or item description) is ranked against a synthetic retailer
/// policy corpus by <see cref="IPolicyRetriever"/>; the top-ranked policy governs
/// the decision and supplies a citation, so a restricted category — hygiene, food,
/// serialized electronics — is refused regardless of physical condition and every
/// decision is grounded in cited policy text rather than an LLM opinion.
/// </summary>
public class RetailerPolicyService : IRetailerPolicyService
{
    /// <summary>
    /// Minimum retrieval similarity to trust a policy hit. Below this the item
    /// falls back to the default "return to seller pending review" policy instead
    /// of being grounded on a weak, possibly wrong match.
    /// </summary>
    private const double MinRetrievalScore = 0.10;

    private readonly IPolicyRetriever _retriever;
    private readonly ILogger<RetailerPolicyService> _logger;

    public RetailerPolicyService(ILogger<RetailerPolicyService> logger, IPolicyRetriever retriever)
    {
        _logger = logger;
        _retriever = retriever;
    }

    public PolicyComplianceResult Evaluate(string category)
    {
        var (entry, score) = Resolve(category);
        var restricted = !entry.ResaleAllowed;

        if (restricted)
            _logger.LogInformation("Policy block for '{Category}' via {PolicyRef} (retrieval {Score:F3}): {Rationale}",
                category, entry.PolicyRef, score, entry.Text);

        return new PolicyComplianceResult
        {
            ResaleAllowed = entry.ResaleAllowed,
            IsRestrictedCategory = restricted,
            PolicyRef = entry.PolicyRef,
            PolicyName = entry.PolicyName,
            Reason = entry.Text,
            RetrievalScore = score,
            RetrievedSnippet = entry.Text
        };
    }

    public Citation GetCitation(string category)
    {
        var (entry, _) = Resolve(category);
        return new Citation("policy", entry.PolicyRef, entry.Text);
    }

    private (PolicyDocument Entry, double Score) Resolve(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return (SyntheticPolicyCorpus.Default, 0);

        var matches = _retriever.Retrieve(category, topK: 1);
        var top = matches.Count > 0 ? matches[0] : null;

        if (top is null || top.Score < MinRetrievalScore)
            return (SyntheticPolicyCorpus.Default, top?.Score ?? 0);

        return (top.Document, top.Score);
    }
}
