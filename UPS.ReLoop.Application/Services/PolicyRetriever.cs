namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// In-process TF-IDF + cosine-similarity retriever over the synthetic policy
/// corpus. This is the retrieval half of the policy RAG pipeline: it turns a
/// query (category or item description) into a ranked list of cited policy
/// documents. It is deterministic and needs no network, so the demo works
/// offline; swap in an embeddings-backed implementation of
/// <see cref="IPolicyRetriever"/> for production semantic search.
/// </summary>
public sealed class PolicyRetriever : IPolicyRetriever
{
    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', ',', '.', ';', ':', '/', '\\', '-', '_', '(', ')', '"', '\'', '!', '?'];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "of", "to", "in", "for", "is", "are", "may",
        "be", "with", "on", "as", "by", "at", "not", "no", "can", "must", "after",
        "returned", "return", "item", "items", "condition", "resale", "resold", "local",
    };

    private readonly ILogger<PolicyRetriever> _logger;
    private readonly IReadOnlyList<PolicyDocument> _docs;
    private readonly Dictionary<string, double> _idf;
    private readonly List<Dictionary<string, double>> _docVectors;

    public PolicyRetriever(ILogger<PolicyRetriever> logger)
        : this(logger, SyntheticPolicyCorpus.Documents)
    {
    }

    public PolicyRetriever(ILogger<PolicyRetriever> logger, IReadOnlyList<PolicyDocument> documents)
    {
        _logger = logger;
        _docs = documents;

        var docTokens = _docs
            .Select(d => Tokenize($"{d.Category} {d.PolicyName} {d.Text}"))
            .ToList();

        var documentFrequency = new Dictionary<string, int>();
        foreach (var tokens in docTokens)
            foreach (var term in tokens.Distinct())
                documentFrequency[term] = documentFrequency.GetValueOrDefault(term) + 1;

        var n = _docs.Count;
        _idf = documentFrequency.ToDictionary(
            kv => kv.Key,
            kv => Math.Log((double)(n + 1) / (kv.Value + 1)) + 1.0);

        _docVectors = docTokens.Select(BuildVector).ToList();

        _logger.LogInformation("Policy RAG index built: {DocCount} documents, {TermCount} terms.",
            _docs.Count, _idf.Count);
    }

    public IReadOnlyList<PolicyRetrievalMatch> Retrieve(string query, int topK = 3)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var queryVector = BuildVector(Tokenize(query));
        if (queryVector.Count == 0)
            return [];

        var matches = new List<PolicyRetrievalMatch>(_docs.Count);
        for (var i = 0; i < _docs.Count; i++)
        {
            var score = Dot(queryVector, _docVectors[i]);
            if (score > 0)
                matches.Add(new PolicyRetrievalMatch(_docs[i], Math.Round(score, 4)));
        }

        return matches
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.Document.PolicyRef, StringComparer.Ordinal)
            .Take(Math.Max(1, topK))
            .ToList();
    }

    private Dictionary<string, double> BuildVector(List<string> tokens)
    {
        var termFrequency = new Dictionary<string, double>();
        foreach (var term in tokens)
            termFrequency[term] = termFrequency.GetValueOrDefault(term) + 1;

        var vector = new Dictionary<string, double>(termFrequency.Count);
        var unseenIdf = Math.Log(_docs.Count + 1) + 1.0;
        foreach (var (term, tf) in termFrequency)
            vector[term] = tf * _idf.GetValueOrDefault(term, unseenIdf);

        var norm = Math.Sqrt(vector.Values.Sum(v => v * v));
        if (norm > 0)
            foreach (var term in vector.Keys.ToList())
                vector[term] /= norm;

        return vector;
    }

    private static double Dot(Dictionary<string, double> a, Dictionary<string, double> b)
    {
        // Both vectors are L2-normalized, so the dot product is the cosine similarity.
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        var dot = 0.0;
        foreach (var (term, weight) in small)
            if (large.TryGetValue(term, out var other))
                dot += weight * other;
        return dot;
    }

    private static List<string> Tokenize(string text)
    {
        var words = text.ToLowerInvariant().Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var tokens = new List<string>(words.Length);
        foreach (var word in words)
        {
            if (word.Length < 3 || StopWords.Contains(word))
                continue;

            // Light stemming: drop a trailing plural 's'.
            var stem = word.EndsWith('s') && word.Length > 3 ? word[..^1] : word;
            tokens.Add(stem);
        }

        return tokens;
    }
}
