namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.DTOs.RootCauseAgent;
using UPS.ReLoop.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class RootCauseAgentController : ControllerBase
{
    private readonly IRootCauseAgentService _service;

    public RootCauseAgentController(IRootCauseAgentService service)
    {
        _service = service;
    }

    /// <summary>
    /// Analyzes a list of returns to identify the root cause pattern.
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] RootCauseRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AnalyzeAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
