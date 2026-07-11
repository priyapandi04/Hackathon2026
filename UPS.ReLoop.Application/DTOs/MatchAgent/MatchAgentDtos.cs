namespace UPS.ReLoop.Application.DTOs.MatchAgent;

public record MatchAgentRequest(
    string ProductId,
    string ProductName,
    string Category,
    string Location,
    string Condition);

public class MatchAgentResponse
{
    public int MatchScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double DistanceSavedKm { get; set; }
    public double CostSaved { get; set; }
    public double Co2Saved { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public List<MatchDetail> MatchDetails { get; set; } = [];
}

public class MatchDetail
{
    public string Factor { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
}
