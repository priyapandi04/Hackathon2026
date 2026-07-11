namespace UPS.ReLoop.Domain.Entities;

using UPS.ReLoop.Domain.Common;

public class ReturnRequest : BaseEntity
{
    public Guid PackageId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? AiAnalysis { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Package Package { get; set; } = null!;
}
