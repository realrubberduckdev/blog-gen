using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using System.ClientModel;
using BlogPostGenerator.Models;
using BlogPostGenerator.Services;
using BlogPostGenerator.Services.AgentProvider;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BlogPostGenerator.Agents;

namespace BlogPostGenerator;

/// <summary>
/// Program entry point with Microsoft Agent Framework configuration
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configuration
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .AddCommandLine(args);

        // Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Configure IChatClient using Azure OpenAI or Google Gemini
        builder.Services.AddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Try local model configuration first
            var useLocal = configuration.GetValue<bool>("LocalModel:UseLocal");
            if (useLocal)
            {
                var localEndpoint = configuration["LocalModel:Endpoint"];
                var localModelName = configuration["LocalModel:ModelName"];
                var localApiKey = configuration["LocalModel:ApiKey"];

                if (string.IsNullOrEmpty(localEndpoint) || string.IsNullOrEmpty(localModelName))
                {
                    logger.LogError("Error: Local model configuration is incomplete.");
                    logger.LogError("Endpoint: {EndpointStatus}", string.IsNullOrEmpty(localEndpoint) ? "missing" : "present");
                    logger.LogError("ModelName: {ModelNameStatus}", string.IsNullOrEmpty(localModelName) ? "missing" : "present");
                    Environment.Exit(1);
                }

                logger.LogInformation("Configuring for local model at: {LocalEndpoint}", localEndpoint);
                return new LocalChatClient(
                    new HttpClient()
                    {
                        Timeout = TimeSpan.FromMinutes(10) // because local models can be slow
                    },
                    localModelName,
                    localEndpoint);
            }

            // Try Google Gemini configuration
            var geminiApiKey = configuration["GoogleAI:ApiKey"];
            var geminiModelId = configuration["GoogleAI:ModelId"] ?? "gemini-2.5-flash"; // list of available models https://ai.google.dev/gemini-api/docs/models

            if (!string.IsNullOrEmpty(geminiApiKey))
            {
                logger.LogInformation("Configuring Google Gemini with model: {GeminiModelId}", geminiModelId);
                var httpClient = new HttpClient();
                return new Services.Gemini.GeminiChatClient(httpClient, geminiApiKey, geminiModelId);
            }

            // Try Azure OpenAI configuration next
            var azureEndpoint = configuration["AzureOpenAI:Endpoint"];
            var azureDeploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";
            var azureApiKey = configuration["AzureOpenAI:ApiKey"];

            if (!string.IsNullOrEmpty(azureEndpoint))
            {
                logger.LogInformation("Configuring Azure OpenAI with endpoint: {AzureEndpoint}", azureEndpoint);

                if (!string.IsNullOrEmpty(azureApiKey))
                {
                    logger.LogInformation("Using API key authentication for Azure OpenAI");

                    // Use API key authentication
                    return new AzureOpenAIClient(new Uri(azureEndpoint), new ApiKeyCredential(azureApiKey))
                        .AsChatClient(azureDeploymentName);
                }
                else
                {
                    logger.LogInformation("Using DefaultAzureCredential for Azure OpenAI authentication");

                    // Use Azure CLI credentials (default)
                    return new AzureOpenAIClient(new Uri(azureEndpoint), new DefaultAzureCredential())
                        .AsChatClient(azureDeploymentName);
                }
            }

            // Fallback to OpenAI
            var openAIApiKey = configuration["OpenAI:ApiKey"];
            if (!string.IsNullOrEmpty(openAIApiKey))
            {
                logger.LogInformation("Configuring OpenAI");
                return new OpenAI.OpenAIClient(openAIApiKey).AsChatClient("gpt-4o-mini");
            }

            throw new InvalidOperationException("No valid AI configuration found. Please configure Google Gemini, Azure OpenAI, or OpenAI settings.");
        });

        // Register Agents
        builder.Services.AddTransient<ResearchAgent>();
        builder.Services.AddTransient<ContentWriterAgent>();
        builder.Services.AddTransient<EditorAgent>();
        builder.Services.AddTransient<MarkdownLinterAgent>();
        builder.Services.AddTransient<SEOAgent>();

        // Register AgentRegistry
        builder.Services.AddTransient<IAgentRegistry, AgentRegistry>();

        // Register the blog generation service
        builder.Services.AddTransient<BlogGenerationService>();

        var host = builder.Build();

        // Run the application
        var blogService = host.Services.GetRequiredService<BlogGenerationService>();
        await blogService.RunAsync(args);
    }
}