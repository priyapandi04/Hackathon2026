namespace UPS.ReLoop.Application.DTOs.MatchAgent;

public record MatchAgentRequest(
    string ProductId,
    string ProductName,
    string Category,
    string Location,
    string Condition,
    decimal SalePrice = 0,
    Guid? ReturnRequestId = null);

public class MatchAgentResponse
{
    public int MatchScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    /// <summary>Match confidence on a 0-1 scale (shared convention with image and decision confidence).</summary>
    public double Confidence { get; set; }
    public double DistanceSavedKm { get; set; }
    public double CostSaved { get; set; }
    public double Co2Saved { get; set; }
    public string Explanation { get; set; } = string.Empty;

    /// <summary>Local resale channel (nearest UPS Store / Access Point) — playbook Agent 1 field.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Deterministic expected days-to-sell within the holding window — playbook Agent 1 field.</summary>
    public int ExpectedDaysToSell { get; set; }

    public List<MatchDetail> MatchDetails { get; set; } = [];
}

public class MatchDetail
{
    public string Factor { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
}
