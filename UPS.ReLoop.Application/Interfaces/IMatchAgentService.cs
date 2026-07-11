namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.MatchAgent;

public interface IMatchAgentService
{
    Task<ApiResponse<MatchAgentResponse>> FindLocalMatchAsync(MatchAgentRequest request, CancellationToken cancellationToken = default);
}
