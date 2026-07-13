namespace UPS.ReLoop.Domain.Entities;

using UPS.ReLoop.Domain.Common;

public class MatchAgentResult : BaseEntity
{
    public Guid ReturnRequestId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double DistanceSavedKm { get; set; }
    public double CostSaved { get; set; }
    public double Co2Saved { get; set; }
    // Persisted triple-value economics (INR) so historical aggregates can show the
    // full net value, not just reverse-freight avoided. Computed by RevenueCalculator.
    public decimal SalePrice { get; set; }
    public decimal ResaleMargin { get; set; }
    public decimal ResaleServiceFee { get; set; }
    public decimal Co2Value { get; set; }
    public decimal NetValue { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string MatchDetailsJson { get; set; } = "[]";
    public ReturnRequest ReturnRequest { get; set; } = null!;
}
