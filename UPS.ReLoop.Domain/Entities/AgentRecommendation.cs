namespace UPS.ReLoop.Domain.Entities;

using UPS.ReLoop.Domain.Common;

public class AgentRecommendation : BaseEntity
{
    public string AgentName { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
