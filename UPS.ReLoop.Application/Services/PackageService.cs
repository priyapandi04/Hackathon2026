namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.Common.Exceptions;
using UPS.ReLoop.Application.DTOs.Package;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Domain.Entities;
using UPS.ReLoop.Domain.Interfaces;

public class PackageService : IPackageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PackageService> _logger;

    public PackageService(IUnitOfWork unitOfWork, ILogger<PackageService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PackageResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Repository<Package>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Package), id);

        return ApiResponse<PackageResponseDto>.SuccessResponse(MapToDto(package));
    }

    public async Task<ApiResponse<IReadOnlyList<PackageResponseDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var packages = await _unitOfWork.Repository<Package>().GetAllAsync(cancellationToken);
        var result = packages.Select(MapToDto).ToList().AsReadOnly();
        return ApiResponse<IReadOnlyList<PackageResponseDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<PackageResponseDto>> CreateAsync(CreatePackageDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Repository<Package>()
            .FindAsync(p => p.TrackingNumber == dto.TrackingNumber, cancellationToken);

        if (existing.Any())
            throw new ConflictException($"Package with tracking number '{dto.TrackingNumber}' already exists.");

        var package = new Package
        {
            TrackingNumber = dto.TrackingNumber,
            SenderName = dto.SenderName,
            SenderAddress = dto.SenderAddress,
            RecipientName = dto.RecipientName,
            RecipientAddress = dto.RecipientAddress,
            Weight = dto.Weight,
            Status = "Created",
            IsReturnable = dto.IsReturnable
        };

        await _unitOfWork.Repository<Package>().AddAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Package created with tracking number {TrackingNumber}", package.TrackingNumber);

        return ApiResponse<PackageResponseDto>.SuccessResponse(MapToDto(package), "Package created successfully", 201);
    }

    public async Task<ApiResponse<PackageResponseDto>> UpdateAsync(Guid id, UpdatePackageDto dto, CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Repository<Package>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Package), id);

        if (dto.SenderName is not null) package.SenderName = dto.SenderName;
        if (dto.SenderAddress is not null) package.SenderAddress = dto.SenderAddress;
        if (dto.RecipientName is not null) package.RecipientName = dto.RecipientName;
        if (dto.RecipientAddress is not null) package.RecipientAddress = dto.RecipientAddress;
        if (dto.Weight.HasValue) package.Weight = dto.Weight.Value;
        if (dto.Status is not null) package.Status = dto.Status;
        if (dto.IsReturnable.HasValue) package.IsReturnable = dto.IsReturnable.Value;

        package.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Package>().UpdateAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Package {Id} updated", id);

        return ApiResponse<PackageResponseDto>.SuccessResponse(MapToDto(package), "Package updated successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Repository<Package>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Package), id);

        await _unitOfWork.Repository<Package>().DeleteAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Package {Id} deleted", id);

        return ApiResponse<bool>.SuccessResponse(true, "Package deleted successfully");
    }

    public async Task<ApiResponse<PackageResponseDto>> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        var packages = await _unitOfWork.Repository<Package>()
            .FindAsync(p => p.TrackingNumber == trackingNumber, cancellationToken);

        var package = packages.FirstOrDefault()
            ?? throw new NotFoundException(nameof(Package), trackingNumber);

        return ApiResponse<PackageResponseDto>.SuccessResponse(MapToDto(package));
    }

    private static PackageResponseDto MapToDto(Package package) => new(
        package.Id,
        package.TrackingNumber,
        package.SenderName,
        package.SenderAddress,
        package.RecipientName,
        package.RecipientAddress,
        package.Weight,
        package.Status,
        package.AiRecommendation,
        package.IsReturnable,
        package.CreatedAt);
}
