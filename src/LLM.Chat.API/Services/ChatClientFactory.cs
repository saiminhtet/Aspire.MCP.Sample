using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using System.ClientModel;

namespace LLM.Chat.API.Services;

public class ChatClientFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatClientFactory> _logger;

    public ChatClientFactory(IConfiguration configuration, ILogger<ChatClientFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IChatClient CreateChatClient()
    {
        var endpoint = _configuration["endpoint"] ?? throw new ArgumentNullException("Endpoint configuration is missing");
        var apiKey = _configuration["apikey"] ?? string.Empty;
        var deploymentName = _configuration["deploymentname"] ?? "llama3.2";

        _logger.LogInformation($"Creating chat client - Endpoint: {endpoint}, DeploymentName: {deploymentName}");

        // Ollama (localhost)
        if (endpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Using Ollama endpoint");
            return new OllamaChatClient(
                    endpoint: new Uri(endpoint),
                    modelId: deploymentName)
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        }

        // OpenAI API
        if (endpoint.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Using OpenAI API");
            var chatClientOptions = new OpenAI.OpenAIClientOptions
            {
                NetworkTimeout = TimeSpan.FromMinutes(5)
            };

            var openAIClient = new OpenAI.OpenAIClient(new ApiKeyCredential(apiKey), chatClientOptions);
            return openAIClient.GetChatClient(deploymentName)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        }

        // GitHub Models or Azure AI Foundry
        _logger.LogInformation("Using Azure AI Inference endpoint");
        var azureClient = new ChatCompletionsClient(
            endpoint: new Uri(endpoint),
            new AzureKeyCredential(apiKey));

        return azureClient.AsIChatClient(deploymentName)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}
