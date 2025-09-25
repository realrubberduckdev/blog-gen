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
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        // Get OpenAI API key from user secrets
        var openAiApiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException("OpenAI API key not found. Please set it using: dotnet user-secrets set \"OpenAI:ApiKey\" \"your-api-key-here\"");
        }

        // Configure services
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Configure Semantic Kernel
        var builder = Kernel.CreateBuilder();

        var endpoint = "https://dp-openai1.openai.azure.com/";
        var deploymentName = "gpt-4o-mini";
        builder.AddAzureOpenAIChatCompletion(
            deploymentName,
            endpoint,
            openAiApiKey);

        var kernel = builder.Build();

        // Register agents
        services.AddSingleton(kernel);
        services.AddTransient<ResearchAgent>();
        services.AddTransient<ContentWriterAgent>();
        services.AddTransient<SEOAgent>();
        services.AddTransient<EditorAgent>();
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

            // Display results
            Console.WriteLine($"TITLE: {result.Title}");
            Console.WriteLine($"META DESCRIPTION: {result.MetaDescription}");
            Console.WriteLine($"TAGS: {string.Join(", ", result.Tags)}");
            Console.WriteLine($"SUMMARY: {result.Summary}");
            Console.WriteLine(new string('=', 50));
            Console.WriteLine("CONTENT:");
            Console.WriteLine(result.Content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}