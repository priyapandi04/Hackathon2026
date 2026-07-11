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
