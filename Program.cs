using BlogPostGenerator.Agents;
using BlogPostGenerator.Models;
using BlogPostGenerator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace BlogPostGenerator;

/// <summary>
/// Program entry point and configuration
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .Build();        // Get configuration settings and configure Semantic Kernel
        var useLocal = configuration.GetValue<bool>("LocalModel:UseLocal");

        // Configure services
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Configure Semantic Kernel
        var builder = Kernel.CreateBuilder();

        if (useLocal)
        {
            // Configure for local model
            var endpoint = configuration["LocalModel:Endpoint"];
            var modelName = configuration["LocalModel:ModelName"];
            var apiKey = configuration["LocalModel:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(modelName) || string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Error: LocalModel configuration is incomplete. Please check your appsettings.json file.");
                Console.WriteLine($"Endpoint: {(string.IsNullOrEmpty(endpoint) ? "missing" : "present")}");
                Console.WriteLine($"ModelName: {(string.IsNullOrEmpty(modelName) ? "missing" : "present")}");
                Console.WriteLine($"ApiKey: {(string.IsNullOrEmpty(apiKey) ? "missing" : "present")}");
                Environment.Exit(1);
            }

            Console.WriteLine($"Configuring for local model at: {endpoint}");
            Console.WriteLine($"Model: {modelName}");

            // Create HttpClient with longer timeout
            var httpClient = new HttpClient
            {
                // Increase timeout as local runs take long
                Timeout = TimeSpan.FromMinutes(10)
            };

            builder.AddOpenAIChatCompletion(
                modelId: modelName,
                apiKey: apiKey,
                endpoint: new Uri(endpoint),
                httpClient: httpClient);
        }
        else
        {
            // Configure for Azure OpenAI
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var deploymentName = configuration["AzureOpenAI:DeploymentName"];
            var apiKey = configuration["AzureOpenAI:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Error: Azure OpenAI configuration is incomplete. Please check your appsettings.json file and user secrets.");
                Console.WriteLine($"Endpoint: {(string.IsNullOrEmpty(endpoint) ? "missing" : "present")}");
                Console.WriteLine($"DeploymentName: {(string.IsNullOrEmpty(deploymentName) ? "missing" : "present")}");
                Console.WriteLine($"ApiKey: {(string.IsNullOrEmpty(apiKey) ? "missing" : "present")}");
                Environment.Exit(1);
            }

            builder.AddAzureOpenAIChatCompletion(
                deploymentName,
                endpoint,
                apiKey);
        }

        var kernel = builder.Build();

        // Register agents
        services.AddSingleton(kernel);
        services.AddTransient<ResearchAgent>();
        services.AddTransient<ContentWriterAgent>();
        services.AddTransient<SEOAgent>();
        services.AddTransient<EditorAgent>();
        services.AddTransient<MarkdownLinterAgent>();
        services.AddTransient<BlogPostOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<BlogPostOrchestrator>();

        // Example usage
        var request = new BlogPostRequest
        {
            Topic = "Avoid Storing Secrets in PowerShell's Command History",
            Description = "using PowerShell scripts to manage secrets like API tokens, passwords, or other sensitive data, you may think you’re safe by using environment variables or script parameters like $PAT or $SomePassword to avoid hardcoding secrets in your scripts",
            TargetAudience = "Software Engineers and DevOps Professionals",
            WordCount = 1000,
            Tone = "Professional"
        };

        Console.WriteLine("Generating blog post...");
        Console.WriteLine($"Topic: {request.Topic}");
        Console.WriteLine($"Description: {request.Description}");
        Console.WriteLine(new string('-', 50));

        try
        {
            var result = await orchestrator.GenerateBlogPostAsync(request);

            // Generate filename from title (safe for filesystem)
            var fileName = $"{DateTime.Now:yyyy-MM-dd}-{result.Title.Replace(" ", "-").Replace(":", "").Replace("?", "").Replace("/", "-").Replace("\\", "-").ToLower()}.md";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            // Create markdown content
            var markdownContent = $"""
            ---
            layout: post
            title: {result.Title}
            image: img/banner.jpg
            author: Dushyant
            date: {DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}
            tags: [{string.Join(", ", result.Tags.Select(tag => $"\"{tag}\""))}]
            draft: false
            ---

            ## {result.Summary}

            {result.Content}
            """;

            // Write to file
            await File.WriteAllTextAsync(filePath, markdownContent);
            Console.WriteLine($"Blog post generated successfully!");
            Console.WriteLine($"File saved as: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}