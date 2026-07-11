namespace UPS.ReLoop.Application.Interfaces;

public interface IOpenAIService
{
    Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> AnalyzeImageAsync(string imageBase64, string prompt, CancellationToken cancellationToken = default);
}
