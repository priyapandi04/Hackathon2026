namespace UPS.ReLoop.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using UPS.ReLoop.Application.DTOs.ImageValidation;
using UPS.ReLoop.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class ImageValidationController : ControllerBase
{
    private readonly IImageValidationService _imageValidationService;

    public ImageValidationController(IImageValidationService imageValidationService)
    {
        _imageValidationService = imageValidationService;
    }

    /// <summary>
    /// Validates a returned product image using Azure OpenAI GPT-4o Vision.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Validate([FromBody] ImageValidationRequest request, CancellationToken cancellationToken)
    {
        var result = await _imageValidationService.ValidateImageAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
