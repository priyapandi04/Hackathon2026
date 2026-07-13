namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Buyers;

public interface IBuyerService
{
    Task<ApiResponse<BuyerListResponse>> GetByHubAsync(string hub, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<string>>> GetAvailableHubsAsync(CancellationToken cancellationToken = default);
}
