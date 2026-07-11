namespace UPS.ReLoop.Infrastructure.Services;

using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using UPS.ReLoop.Application.Interfaces;
using UPS.ReLoop.Infrastructure.Configuration;

public class AzureOpenAiService : IAiService, IOpenAIService
{
    private readonly Lazy<ChatClient> _chatClient;
    private readonly ILogger<AzureOpenAiService> _logger;

    public AzureOpenAiService(IOptions<AzureOpenAiSettings> settings, ILogger<AzureOpenAiService> logger)
    {
        _logger = logger;
        var config = settings.Value;

        // Build the client lazily so a missing/invalid key does not crash service
        // construction (which would take down the whole pipeline). Instead the error
        // surfaces on the first actual LLM call, where callers fall back gracefully.
        _chatClient = new Lazy<ChatClient>(() => CreateChatClient(config));

        _logger.LogInformation("AI service configured: provider '{Provider}', model '{Model}'",
            config.Provider, config.DeploymentName);
    }

    private ChatClient Chat => _chatClient.Value;

    /// <summary>
    /// Builds a <see cref="ChatClient"/> for either an Azure OpenAI resource or any
    /// OpenAI-compatible endpoint (GitHub Models, Ollama, OpenAI, LM Studio…). The
    /// rest of the service is provider-agnostic because both paths return the same
    /// <see cref="ChatClient"/> type. Throws a clear <see cref="InvalidOperationException"/>
    /// when configuration is missing so the health check reports exactly what to fix.
    /// </summary>
    private static ChatClient CreateChatClient(AzureOpenAiSettings config)
    {
        if (string.IsNullOrWhiteSpace(config.Endpoint))
            throw new InvalidOperationException("AI Endpoint is not configured (AzureOpenAI:Endpoint).");
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new InvalidOperationException("AI ApiKey is not configured. Set AzureOpenAI:ApiKey (e.g. a GitHub token via the AzureOpenAI__ApiKey environment variable).");
        if (string.IsNullOrWhiteSpace(config.DeploymentName))
            throw new InvalidOperationException("AI model is not configured (AzureOpenAI:DeploymentName).");

        var credential = new ApiKeyCredential(config.ApiKey);

        if (config.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
        {
            var azureClient = new AzureOpenAIClient(new Uri(config.Endpoint), credential);
            return azureClient.GetChatClient(config.DeploymentName);
        }

        // OpenAI-compatible provider: the configured Endpoint is the base URL and
        // the SDK appends /chat/completions (e.g. GitHub Models, local Ollama).
        var openAiClient = new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = new Uri(config.Endpoint) });
        return openAiClient.GetChatClient(config.DeploymentName);
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));

        _logger.LogInformation("Generating text response for prompt of length {PromptLength}", prompt.Length);

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful AI assistant for UPS ReLoop Nexus. Provide clear and concise responses."),
                new UserChatMessage(prompt)
            };

            var completion = await Chat.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var result = completion.Value.Content[0].Text;

            _logger.LogInformation("Text generation completed. Response length: {ResponseLength}", result.Length);
            return result;
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "Azure OpenAI API error during text generation. Status: {Status}", ex.Status);
            throw new InvalidOperationException($"Azure OpenAI request failed with status {ex.Status}.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error during text generation");
            throw new InvalidOperationException("An unexpected error occurred while generating text.", ex);
        }
    }

    public async Task<string> AnalyzeImageAsync(string imageBase64, string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageBase64, nameof(imageBase64));
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));

        _logger.LogInformation("Analyzing image with prompt of length {PromptLength}", prompt.Length);

        try
        {
            var imageBytes = Convert.FromBase64String(imageBase64);
            var imageData = BinaryData.FromBytes(imageBytes);

            var imagePart = ChatMessageContentPart.CreateImagePart(imageData, "image/png");
            var textPart = ChatMessageContentPart.CreateTextPart(prompt);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful AI assistant for UPS ReLoop Nexus with vision capabilities. Analyze images and provide detailed, actionable insights."),
                new UserChatMessage(new List<ChatMessageContentPart> { textPart, imagePart })
            };

            var completion = await Chat.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var result = completion.Value.Content[0].Text;

            _logger.LogInformation("Image analysis completed. Response length: {ResponseLength}", result.Length);
            return result;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid Base64 image data provided");
            throw new ArgumentException("The provided image data is not valid Base64.", nameof(imageBase64), ex);
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "Azure OpenAI API error during image analysis. Status: {Status}", ex.Status);
            throw new InvalidOperationException($"Azure OpenAI request failed with status {ex.Status}.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error during image analysis");
            throw new InvalidOperationException("An unexpected error occurred while analyzing the image.", ex);
        }
    }

    public async Task<string> AnalyzeReturnRequestAsync(string reason, string packageDetails, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing return request for package details of length {Length}", packageDetails.Length);

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a UPS return logistics AI assistant. Analyze return requests and provide recommendations for processing. Be concise."),
                new UserChatMessage($"Package Details: {packageDetails}\nReturn Reason: {reason}\n\nProvide analysis and recommendation for this return request.")
            };

            var completion = await Chat.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return completion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI for return analysis");
            return "AI analysis unavailable at this time.";
        }
    }

    public async Task<string> GetPackageRecommendationAsync(string packageDetails, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting package recommendation for details of length {Length}", packageDetails.Length);

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a UPS logistics optimization AI. Provide routing and handling recommendations. Be concise."),
                new UserChatMessage($"Package Details: {packageDetails}\n\nProvide optimization recommendations.")
            };

            var completion = await Chat.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return completion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI for package recommendation");
            return "AI recommendation unavailable at this time.";
        }
    }
}
