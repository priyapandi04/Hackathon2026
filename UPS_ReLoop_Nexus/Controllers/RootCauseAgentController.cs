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

    /// <summary>
    /// Clusters many returns into systemic root causes and priced retailer
    /// fix-tickets (the Reduce pillar — lowers return volume at the source).
    /// </summary>
    [HttpPost("cluster")]
    public IActionResult Cluster([FromBody] RootCauseRequest request)
    {
        var result = _service.ClusterReturns(request);
        return StatusCode(result.StatusCode, result);
    }
}
