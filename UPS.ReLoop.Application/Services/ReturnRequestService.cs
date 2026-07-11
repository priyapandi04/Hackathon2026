namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.Common.Exceptions;
using UPS.ReLoop.Application.DTOs.ReturnRequest;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;
using UPS.ReLoop.Domain.Entities;
using UPS.ReLoop.Domain.Interfaces;

public class ReturnRequestService : IReturnRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiService _aiService;
    private readonly IReturnRequestSpRepository _spRepository;
    private readonly ILogger<ReturnRequestService> _logger;

    public ReturnRequestService(IUnitOfWork unitOfWork, IAiService aiService, IReturnRequestSpRepository spRepository, ILogger<ReturnRequestService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _spRepository = spRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<ReturnRequestResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.Repository<ReturnRequest>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ReturnRequest), id);

        return ApiResponse<ReturnRequestResponseDto>.SuccessResponse(MapToDto(request));
    }

    public async Task<ApiResponse<IReadOnlyList<ReturnRequestResponseDto>>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var requests = await _unitOfWork.Repository<ReturnRequest>()
            .FindAsync(r => r.PackageId == packageId, cancellationToken);

        var result = requests.Select(MapToDto).ToList().AsReadOnly();
        return ApiResponse<IReadOnlyList<ReturnRequestResponseDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<ReturnRequestResponseDto>> CreateAsync(CreateReturnRequestDto dto, CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Repository<Package>().GetByIdAsync(dto.PackageId, cancellationToken)
            ?? throw new NotFoundException(nameof(Package), dto.PackageId);

        if (!package.IsReturnable)
            throw new BadRequestException("This package is not eligible for return.");

        var packageDetails = $"Tracking: {package.TrackingNumber}, Weight: {package.Weight}kg, Status: {package.Status}";
        var aiAnalysis = await _aiService.AnalyzeReturnRequestAsync(dto.Reason, packageDetails, cancellationToken);

        var returnRequest = new ReturnRequest
        {
            PackageId = dto.PackageId,
            Reason = dto.Reason,
            AiAnalysis = aiAnalysis,
            Status = "Pending"
        };

        await _unitOfWork.Repository<ReturnRequest>().AddAsync(returnRequest, cancellationToken);

        package.ReturnInitiatedAt = DateTime.UtcNow;
        package.Status = "Return Initiated";
        await _unitOfWork.Repository<Package>().UpdateAsync(package, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Return request created for package {PackageId}", dto.PackageId);

        return ApiResponse<ReturnRequestResponseDto>.SuccessResponse(MapToDto(returnRequest), "Return request created successfully", 201);
    }

    public async Task<ApiResponse<CreateReturnRequestSpResponse>> CreateViaSpAsync(CreateReturnRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.PackageId == Guid.Empty)
            throw new BadRequestException("PackageId is required.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BadRequestException("Return reason is required.");

        _logger.LogInformation("Creating return request via SP for PackageId: {PackageId}", dto.PackageId);

        var result = await _spRepository.CreateAsync(dto.PackageId, dto.Reason, dto.Location, dto.ImageUrl, cancellationToken);

        if (result is null)
            return ApiResponse<CreateReturnRequestSpResponse>.FailResponse("Failed to create return request.", 500);

        _logger.LogInformation("Return request created via SP: {ReturnRequestId}", result.ReturnRequestId);

        return ApiResponse<CreateReturnRequestSpResponse>.SuccessResponse(result, "Return request created successfully.", 201);
    }

    public async Task<ApiResponse<ReturnRequestResponseDto>> ResolveAsync(Guid id, string resolutionNotes, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.Repository<ReturnRequest>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ReturnRequest), id);

        request.Status = "Resolved";
        request.ResolutionNotes = resolutionNotes;
        request.ResolvedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<ReturnRequest>().UpdateAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Return request {Id} resolved", id);

        return ApiResponse<ReturnRequestResponseDto>.SuccessResponse(MapToDto(request), "Return request resolved successfully");
    }

    private static ReturnRequestResponseDto MapToDto(ReturnRequest r) => new(
        r.Id,
        r.PackageId,
        r.Reason,
        r.Status,
        r.AiAnalysis,
        r.ResolutionNotes,
        r.CreatedAt,
        r.ResolvedAt);
}
