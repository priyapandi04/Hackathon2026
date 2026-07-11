namespace UPS.ReLoop.Domain.Entities;

using UPS.ReLoop.Domain.Common;

public class Package : BaseEntity
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AiRecommendation { get; set; }
    public bool IsReturnable { get; set; }
    public DateTime? ReturnInitiatedAt { get; set; }
    public ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
}
