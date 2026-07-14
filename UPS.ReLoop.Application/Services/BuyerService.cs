namespace UPS.ReLoop.Application.Services;

using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Buyers;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Application.Interfaces.Repositories;

public class BuyerService : IBuyerService
{
    private static readonly List<string> AvailableHubs = ["CHN", "BLR", "MUM", "DEL", "HYD"];

    private readonly IBuyerSpRepository _buyerSpRepo;
    private readonly ILogger<BuyerService> _logger;

    public BuyerService(IBuyerSpRepository buyerSpRepo, ILogger<BuyerService> logger)
    {
        _buyerSpRepo = buyerSpRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<BuyerListResponse>> GetByHubAsync(string hub, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hub))
            return ApiResponse<BuyerListResponse>.FailResponse("Hub parameter is required.", 400);

        var normalizedHub = hub.Trim().ToUpperInvariant();

        try
        {
            var buyers = await _buyerSpRepo.GetByHubAsync(normalizedHub, cancellationToken);

            if (buyers.Count == 0)
                return ApiResponse<BuyerListResponse>.FailResponse(
                    $"Hub '{hub}' not found. Available hubs: {string.Join(", ", AvailableHubs)}.", 404);

            var response = new BuyerListResponse(normalizedHub, buyers.ToList());
            return ApiResponse<BuyerListResponse>.SuccessResponse(response, $"Found {buyers.Count} buyers for hub {normalizedHub}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve buyers for hub {Hub}", normalizedHub);
            return ApiResponse<BuyerListResponse>.FailResponse("Failed to retrieve buyers from database.", 500);
        }
    }

    public Task<ApiResponse<List<string>>> GetAvailableHubsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponse<List<string>>.SuccessResponse(AvailableHubs, $"{AvailableHubs.Count} hubs available."));
    }
}
