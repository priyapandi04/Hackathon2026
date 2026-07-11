namespace UPS.ReLoop.Domain.Entities;

using UPS.ReLoop.Domain.Common;

public class DemandHistory : BaseEntity
{
    public string ProductId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public double DemandScore { get; set; }
}
