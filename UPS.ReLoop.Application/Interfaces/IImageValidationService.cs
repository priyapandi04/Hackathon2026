namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.ImageValidation;

public interface IImageValidationService
{
    Task<ApiResponse<ImageValidationResponse>> ValidateImageAsync(ImageValidationRequest request, CancellationToken cancellationToken = default);
}
