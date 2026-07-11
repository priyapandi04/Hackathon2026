namespace UPS.ReLoop.Application.DTOs.BusinessExplanation;

public record BusinessExplanationRequest(
    string ProductName,
    string? Category = null,
    double? DemandScore = null,
    double? DistanceSavedKm = null,
    double? CostSaved = null,
    double? Co2Saved = null,
    string? Recommendation = null,
    int? MatchScore = null);

public class BusinessExplanationResponse
{
    public string Explanation { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyBenefits { get; set; } = [];
}
