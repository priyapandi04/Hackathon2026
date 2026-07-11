namespace UPS.ReLoop.Application.Interfaces;

public interface IAiService
{
    Task<string> AnalyzeReturnRequestAsync(string reason, string packageDetails, CancellationToken cancellationToken = default);
    Task<string> GetPackageRecommendationAsync(string packageDetails, CancellationToken cancellationToken = default);
}
