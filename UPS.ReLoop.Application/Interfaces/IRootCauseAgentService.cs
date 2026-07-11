namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.RootCauseAgent;

public interface IRootCauseAgentService
{
    Task<ApiResponse<RootCauseAnalysisResult>> AnalyzeAsync(RootCauseRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clusters many returns by category + reason to surface systemic root causes
    /// and generate priced retailer fix-tickets (the Reduce pillar).
    /// </summary>
    ApiResponse<ReturnClusterResult> ClusterReturns(RootCauseRequest request, decimal avgReverseCostPerItem = 9m);
}
