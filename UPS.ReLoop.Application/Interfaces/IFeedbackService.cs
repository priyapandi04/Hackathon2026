namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Feedback;

/// <summary>
/// Captures store-associate Accept / Modify / Reject decisions and exposes the
/// aggregated learning signal (accept-rate, most-corrected fields) that powers
/// the "learns daily" moat.
/// </summary>
public interface IFeedbackService
{
    Task<ApiResponse<AssociateFeedbackResponse>> CaptureAsync(AssociateFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<FeedbackSummary>> GetSummaryAsync(CancellationToken cancellationToken = default);
}
