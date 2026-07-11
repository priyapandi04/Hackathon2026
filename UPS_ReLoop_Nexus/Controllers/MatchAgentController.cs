namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.DTOs.MatchAgent;
using UPS.ReLoop.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class MatchAgentController : ControllerBase
{
    private readonly IMatchAgentService _matchAgentService;

    public MatchAgentController(IMatchAgentService matchAgentService)
    {
        _matchAgentService = matchAgentService;
    }

    /// <summary>
    /// Finds hyperlocal demand match for a returned product.
    /// </summary>
    [HttpPost("find-match")]
    public async Task<IActionResult> FindMatch([FromBody] MatchAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await _matchAgentService.FindLocalMatchAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
