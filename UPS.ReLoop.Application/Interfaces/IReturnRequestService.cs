namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.ReturnRequest;

public interface IReturnRequestService
{
    Task<ApiResponse<ReturnRequestResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<ReturnRequestResponseDto>>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ReturnRequestResponseDto>> CreateAsync(CreateReturnRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<CreateReturnRequestSpResponse>> CreateViaSpAsync(CreateReturnRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ReturnRequestResponseDto>> ResolveAsync(Guid id, string resolutionNotes, CancellationToken cancellationToken = default);
}
