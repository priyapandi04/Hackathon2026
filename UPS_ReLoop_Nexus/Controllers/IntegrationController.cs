namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.DTOs.Integration;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Integration endpoint that orchestrates the full return processing pipeline.
/// Chains: Image Validation ? Hyperlocal Match ? Root Cause Analysis ? Savings Calculation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IntegrationController : ControllerBase
{
    private readonly IReturnProcessingOrchestrator _orchestrator;

    public IntegrationController(IReturnProcessingOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Processes a return through the complete AI agent pipeline.
    /// </summary>
    /// <param name="request">Return processing request with package and product details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete processing result from all agents.</returns>
    /// <response code="200">Return processed successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Package not found.</response>
    [HttpPost("process-return")]
    [ProducesResponseType(typeof(Application.Common.ApiResponse<ReturnProcessingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessReturn(
        [FromBody] ReturnProcessingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orchestrator.ProcessReturnAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
