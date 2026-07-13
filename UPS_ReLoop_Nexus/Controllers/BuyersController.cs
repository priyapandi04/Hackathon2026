namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BuyersController : ControllerBase
{
    private readonly IBuyerService _buyerService;

    public BuyersController(IBuyerService buyerService)
    {
        _buyerService = buyerService;
    }

    /// <summary>
    /// Gets buyers for a specific hub (CHN, BLR, MUM, DEL, HYD).
    /// </summary>
    /// <param name="hub">Hub code (e.g. CHN, BLR, MUM, DEL, HYD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> GetByHub([FromQuery] string hub, CancellationToken cancellationToken)
    {
        var result = await _buyerService.GetByHubAsync(hub, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets all available hub codes.
    /// </summary>
    [HttpGet("hubs")]
    public async Task<IActionResult> GetHubs(CancellationToken cancellationToken)
    {
        var result = await _buyerService.GetAvailableHubsAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
