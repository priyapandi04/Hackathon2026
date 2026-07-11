namespace UPS.ReLoop.Application.Interfaces;

using UPS.ReLoop.Application.Common;
using UPS.ReLoop.Application.DTOs.Integration;

/// <summary>
/// Orchestrates the complete return processing pipeline across all agents.
/// </summary>
public interface IReturnProcessingOrchestrator
{
    /// <summary>
    /// Processes a return through the full pipeline:
    /// 1. Image Validation ? 2. Hyperlocal Match ? 3. Root Cause Analysis ? 4. Savings Calculation
    /// </summary>
    Task<ApiResponse<ReturnProcessingResponse>> ProcessReturnAsync(ReturnProcessingRequest request, CancellationToken cancellationToken = default);
}
