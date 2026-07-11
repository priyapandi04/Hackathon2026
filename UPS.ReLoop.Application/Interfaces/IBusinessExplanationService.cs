namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.BusinessExplanation;

public interface IBusinessExplanationService
{
    Task<ApiResponse<BusinessExplanationResponse>> GenerateExplanationAsync(BusinessExplanationRequest request, CancellationToken cancellationToken = default);
}
