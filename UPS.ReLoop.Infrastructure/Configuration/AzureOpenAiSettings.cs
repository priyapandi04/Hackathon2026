namespace UPS.ReLoop.Infrastructure.Configuration;

public class AzureOpenAiSettings
{
    public const string SectionName = "AzureOpenAI";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}
