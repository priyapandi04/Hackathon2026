namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.DTOs.BusinessExplanation;
using UPS.ReLoop.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class BusinessExplanationController : ControllerBase
{
    private readonly IBusinessExplanationService _service;

    public BusinessExplanationController(IBusinessExplanationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Generates an AI-powered business explanation for a logistics decision.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] BusinessExplanationRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.GenerateExplanationAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
