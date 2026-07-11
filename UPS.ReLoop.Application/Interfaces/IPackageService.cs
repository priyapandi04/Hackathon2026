namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Package;

public interface IPackageService
{
    Task<ApiResponse<PackageResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<PackageResponseDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PackageResponseDto>> CreateAsync(CreatePackageDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<PackageResponseDto>> UpdateAsync(Guid id, UpdatePackageDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PackageResponseDto>> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
}
