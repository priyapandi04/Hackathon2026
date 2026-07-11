namespace UPS.ReLoop.Infrastructure.Configuration;

public class AzureOpenAiSettings
{
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// Which client to build: "Azure" for an Azure OpenAI resource, or any other
    /// value (e.g. "GitHubModels", "Ollama", "OpenAI") for an OpenAI-compatible
    /// endpoint where <see cref="Endpoint"/> is used as the base URL. This lets the
    /// same code run against free providers for local testing.
    /// </summary>
    public string Provider { get; set; } = "Azure";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Azure deployment name, or model id for OpenAI-compatible providers (e.g. "openai/gpt-4o-mini", "llama3.2").</summary>
    public string DeploymentName { get; set; } = string.Empty;
}
