namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.DTOs.Feedback;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Human-in-the-loop feedback endpoints. The store associate Accepts / Modifies /
/// Rejects each AI recommendation; the aggregated signal powers the "learns
/// daily" accept-rate curve on the dashboard.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    /// <summary>Capture an Accept / Modify / Reject decision from the associate console.</summary>
    [HttpPost]
    public async Task<IActionResult> Capture([FromBody] AssociateFeedbackRequest request, CancellationToken cancellationToken)
    {
        var result = await _feedbackService.CaptureAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Aggregated learning signal (accept-rate, most-corrected fields).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var result = await _feedbackService.GetSummaryAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
