namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.DTOs.ReturnRequest;
using UPS.ReLoop.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class ReturnRequestsController : ControllerBase
{
    private readonly IReturnRequestService _returnRequestService;

    public ReturnRequestsController(IReturnRequestService returnRequestService)
    {
        _returnRequestService = returnRequestService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _returnRequestService.GetByIdAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("package/{packageId:guid}")]
    public async Task<IActionResult> GetByPackageId(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await _returnRequestService.GetByPackageIdAsync(packageId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Creates a new return request via stored procedure.
    /// </summary>
    /// <param name="dto">Package ID, reason, optional location and image URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Return request ID, package ID, status, and created date.</returns>
    /// <response code="201">Return request created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UPS.ReLoop.Application.Common.ApiResponse<UPS.ReLoop.Application.DTOs.ReturnRequest.CreateReturnRequestSpResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UPS.ReLoop.Application.Common.ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _returnRequestService.CreateViaSpAsync(dto, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveRequest request, CancellationToken cancellationToken)
    {
        var result = await _returnRequestService.ResolveAsync(id, request.ResolutionNotes, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public record ResolveRequest(string ResolutionNotes);
