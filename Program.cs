using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using System.ClientModel;
using BlogPostGenerator.Models;
using BlogPostGenerator.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        builder.Services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Try local model configuration first
            var useLocal = configuration.GetValue<bool>("LocalModel:UseLocal");
            if (useLocal)
            {
                var localEndpoint = configuration["LocalModel:Endpoint"];
                var localModelName = configuration["LocalModel:ModelName"];
                var localApiKey = configuration["LocalModel:ApiKey"];

                if (string.IsNullOrEmpty(localEndpoint) || string.IsNullOrEmpty(localModelName))
                {
                    Console.WriteLine("Error: Local model configuration is incomplete.");
                    Console.WriteLine($"Endpoint: {(string.IsNullOrEmpty(localEndpoint) ? "missing" : "present")}");
                    Console.WriteLine($"ModelName: {(string.IsNullOrEmpty(localModelName) ? "missing" : "present")}");
                    Environment.Exit(1);
                }

                Console.WriteLine($"Configuring for local model at: {localEndpoint}");
                return new BlogPostGenerator.Services.LocalChatClient(new HttpClient(), localModelName, localEndpoint);
            }

            // Try Google Gemini configuration
            var geminiApiKey = configuration["GoogleAI:ApiKey"];
            var geminiModelId = configuration["GoogleAI:ModelId"] ?? "gemini-2.5-flash";

            if (!string.IsNullOrEmpty(geminiApiKey))
            {
                Console.WriteLine($"Configuring Google Gemini with model: {geminiModelId}");
                var httpClient = new HttpClient();
                return new BlogPostGenerator.Services.Gemini.GeminiChatClient(httpClient, geminiApiKey, geminiModelId);
            }

            // Try Azure OpenAI configuration next
            var azureEndpoint = configuration["AzureOpenAI:Endpoint"];
            var azureDeploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";
            var azureApiKey = configuration["AzureOpenAI:ApiKey"];

            if (!string.IsNullOrEmpty(azureEndpoint))
            {
                Console.WriteLine($"Configuring Azure OpenAI with endpoint: {azureEndpoint}");

                if (!string.IsNullOrEmpty(azureApiKey))
                {
                    // Use API key authentication
                    return new AzureOpenAIClient(new Uri(azureEndpoint), new ApiKeyCredential(azureApiKey))
                        .AsChatClient(azureDeploymentName);
                }
                else
                {
                    // Use Azure CLI credentials (default)
                    return new AzureOpenAIClient(new Uri(azureEndpoint), new DefaultAzureCredential())
                        .AsChatClient(azureDeploymentName);
                }
            }

            // Fallback to OpenAI
            var openAIApiKey = configuration["OpenAI:ApiKey"];
            if (!string.IsNullOrEmpty(openAIApiKey))
            {
                Console.WriteLine("Configuring OpenAI");
                return new OpenAI.OpenAIClient(openAIApiKey).AsChatClient("gpt-4o-mini");
            }

            throw new InvalidOperationException("No valid AI configuration found. Please configure Google Gemini, Azure OpenAI, or OpenAI settings.");
        });

        // Register the blog generation service
        builder.Services.AddTransient<BlogGenerationService>();

        var host = builder.Build();

        // Run the application
        var blogService = host.Services.GetRequiredService<BlogGenerationService>();
        await blogService.RunAsync(args);
    }
}