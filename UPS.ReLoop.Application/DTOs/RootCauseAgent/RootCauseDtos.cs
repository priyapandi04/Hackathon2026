namespace UPS.ReLoop.Application.DTOs.RootCauseAgent;

using System.Text.Json.Serialization;

public record RootCauseRequest(List<ReturnItem> Returns);

public record ReturnItem(
    string Category,
    string ProductName,
    string ReturnReason,
    string Location);

public class RootCauseResponse
{
    [JsonPropertyName("rootCause")]
    public string RootCause { get; set; } = string.Empty;

    [JsonPropertyName("frequency")]
    public string Frequency { get; set; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;

    [JsonPropertyName("impact")]
    public string Impact { get; set; } = string.Empty;
}

public class RootCauseAnalysisResult
{
    public RootCauseResponse AiAnalysis { get; set; } = new();
    public List<ReasonBreakdown> ReasonBreakdown { get; set; } = [];
    public int TotalReturns { get; set; }
    public string TopCategory { get; set; } = string.Empty;
    public string TopLocation { get; set; } = string.Empty;
}

public class ReasonBreakdown
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Result of clustering many returns to surface systemic root causes and turn
/// each into a priced retailer fix-ticket ("40% of apparel returns = size-chart
/// error on SKU X -> ~$380K/yr"). This is the Reduce pillar: it lowers return
/// VOLUME at the source, not just handles one return.
/// </summary>
public class ReturnClusterResult
{
    public int TotalReturns { get; set; }
    public List<ReturnCluster> Clusters { get; set; } = [];
}

public class ReturnCluster
{
    public string Category { get; set; } = string.Empty;
    public string DominantReason { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string TopLocation { get; set; } = string.Empty;
    public decimal EstimatedAnnualImpact { get; set; }
    public string FixTicket { get; set; } = string.Empty;
}
