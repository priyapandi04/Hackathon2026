namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.RootCauseAgent;

public interface IRootCauseAgentService
{
    Task<ApiResponse<RootCauseAnalysisResult>> AnalyzeAsync(RootCauseRequest request, CancellationToken cancellationToken = default);
}
