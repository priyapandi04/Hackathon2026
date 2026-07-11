namespace UPS.ReLoop.Application.Interfaces;

/// <summary>
/// One synthetic retailer return/resale policy document in the RAG knowledge base.
/// This is fabricated demo data — no real UPS or retailer policy content.
/// </summary>
public sealed record PolicyDocument(
    string PolicyRef,
    string PolicyName,
    string Category,
    bool ResaleAllowed,
    string Text);

/// <summary>A retrieved policy document with its similarity score (0-1).</summary>
public sealed record PolicyRetrievalMatch(PolicyDocument Document, double Score);

/// <summary>
/// Retrieval step of the policy RAG pipeline. Ranks the synthetic policy corpus
/// against a query (category or free-text item description) so a resale decision
/// is grounded in cited policy text instead of an LLM guess. Runs fully in-process
/// (no external calls) so it works offline for the demo; the same interface can be
/// backed by Azure OpenAI embeddings in production.
/// </summary>
public interface IPolicyRetriever
{
    /// <summary>Return the top-K most relevant policy documents for a query.</summary>
    IReadOnlyList<PolicyRetrievalMatch> Retrieve(string query, int topK = 3);
}
