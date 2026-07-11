namespace UPS.ReLoop.Domain.Entities;

using UPS.ReLoop.Domain.Common;

public class InventoryPool : BaseEntity
{
    public Guid ReturnId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int HoldingDays { get; set; }
    public double MatchScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public Return Return { get; set; } = null!;
}
