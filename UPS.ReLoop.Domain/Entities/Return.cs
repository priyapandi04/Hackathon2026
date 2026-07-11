namespace UPS.ReLoop.Domain.Entities;

using UPS.ReLoop.Domain.Common;

public class Return : BaseEntity
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ReturnReason { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Eligibility { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public ICollection<InventoryPool> InventoryPoolItems { get; set; } = new List<InventoryPool>();
}
