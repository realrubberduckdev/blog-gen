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

    private static void ConfigureChatClient(HostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var useLocal = configuration.GetValue<bool>("LocalModel:UseLocal");

        if (useLocal)
        {
            // Configure for local model
            var endpoint = configuration["LocalModel:Endpoint"];
            var modelName = configuration["LocalModel:ModelName"];
            var apiKey = configuration["LocalModel:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(modelName))
            {
                Console.WriteLine("Error: Local model configuration is incomplete.");
                Console.WriteLine($"Endpoint: {(string.IsNullOrEmpty(endpoint) ? "missing" : "present")}");
                Console.WriteLine($"ModelName: {(string.IsNullOrEmpty(modelName) ? "missing" : "present")}");
                Environment.Exit(1);
            }

            Console.WriteLine($"Configuring for local model at: {endpoint}");
            Console.WriteLine($"Model: {modelName}");

            // For now, use a placeholder implementation until we can resolve the correct API
            builder.Services.AddSingleton<IChatClient>(serviceProvider =>
            {
                throw new NotImplementedException("Local model configuration needs to be implemented with the correct Microsoft.Extensions.AI.OpenAI API");
            });
        }
        else
        {
            // Configure for Azure OpenAI
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var deploymentName = configuration["AzureOpenAI:DeploymentName"];
            var apiKey = configuration["AzureOpenAI:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Error: Azure OpenAI configuration is incomplete.");
                Console.WriteLine($"Endpoint: {(string.IsNullOrEmpty(endpoint) ? "missing" : "present")}");
                Console.WriteLine($"DeploymentName: {(string.IsNullOrEmpty(deploymentName) ? "missing" : "present")}");
                Console.WriteLine($"ApiKey: {(string.IsNullOrEmpty(apiKey) ? "missing" : "present")}");
                Environment.Exit(1);
            }

            // For now, use a placeholder implementation until we can resolve the correct API
            builder.Services.AddSingleton<IChatClient>(serviceProvider =>
            {
                throw new NotImplementedException("Azure OpenAI configuration needs to be implemented with the correct Microsoft.Extensions.AI.OpenAI API");
            });
        }
    }

    private static async Task RunBlogGenerationAsync(IHost host, string[] args)
    {
        using var scope = host.Services.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<BlogPostOrchestrator>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Parse command line arguments, configuration, or use interactive input
        var request = await GetBlogPostRequestAsync(args, configuration);

        Console.WriteLine("Generating blog post with Microsoft Agent Framework...");
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
            <div className="seo-hidden">
            {result.MetaDescription}
            </div>

            {result.Content}
            """;

            // Write to file
            await File.WriteAllTextAsync(filePath, markdownContent);

            Console.WriteLine("\n✅ Blog post generated successfully!");
            Console.WriteLine($"📝 Title: {result.Title}");
            Console.WriteLine($"📊 Word Count: {result.Content.Split(' ').Length}");
            Console.WriteLine($"📝 Meta Description: {result.MetaDescription}");
            
            if (result.Tags.Any())
            {
                Console.WriteLine($"🏷️ Tags: {string.Join(", ", result.Tags)}");
            }

            Console.WriteLine($"💾 File saved as: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Gets blog post request from multiple sources: JSON file, command line args, config file, or interactive input
    /// </summary>
    private static async Task<BlogPostRequest> GetBlogPostRequestAsync(string[] args, IConfiguration configuration)
    {
        // Option 1: Check for JSON file parameter
        if (args.Length > 0)
        {
            // Check if first argument is a JSON file path
            if (args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]))
            {
                Console.WriteLine($"Loading blog post request from: {args[0]}");
                return await LoadFromJsonFileAsync(args[0]);
            }

            // Otherwise try parsing command line arguments
            var request = ParseCommandLineArgs(args);
            if (!string.IsNullOrEmpty(request.Topic))
                return request;
        }

        // Option 2: Look for default request files in current directory
        var defaultFiles = new[] { "blog-request.json", "request.json", "sample-request.json" };
        foreach (var fileName in defaultFiles)
        {
            if (File.Exists(fileName))
            {
                Console.WriteLine($"Found default request file: {fileName}");
                return await LoadFromJsonFileAsync(fileName);
            }
        }

        // Option 3: Try configuration file
        var configRequest = configuration.GetSection("BlogPostDefaults").Get<BlogPostRequest>();
        if (configRequest != null && !string.IsNullOrEmpty(configRequest.Topic))
        {
            Console.WriteLine("Using configuration from appsettings.json");
            return configRequest;
        }

        // Option 4: Interactive input
        Console.WriteLine("No topic provided. Please enter blog post details:");
        return await GetInteractiveInputAsync();
    }

    /// <summary>
    /// Loads BlogPostRequest from a JSON file with error handling and validation
    /// </summary>
    private static async Task<BlogPostRequest> LoadFromJsonFileAsync(string filePath)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var request = JsonSerializer.Deserialize<BlogPostRequest>(jsonContent, options);

            // Validate required fields
            if (request == null || string.IsNullOrWhiteSpace(request.Topic))
            {
                throw new ArgumentException("Topic is required in the JSON file.");
            }

            Console.WriteLine($"✅ Loaded request: {request.Topic}");
            return request;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ Invalid JSON format in {filePath}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading {filePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses command line arguments into BlogPostRequest
    /// Usage: dotnet run --topic "Your Topic" --description "Description" --audience "Developers" --wordcount 1000 --tone "Professional"
    /// </summary>
    private static BlogPostRequest ParseCommandLineArgs(string[] args)
    {
        var request = new BlogPostRequest();

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i].ToLower())
            {
                case "--topic":
                case "-t":
                    request.Topic = args[i + 1];
                    break;
                case "--description":
                case "-d":
                    request.Description = args[i + 1];
                    break;
                case "--audience":
                case "-a":
                    request.TargetAudience = args[i + 1];
                    break;
                case "--wordcount":
                case "-w":
                    if (int.TryParse(args[i + 1], out int wordCount))
                        request.WordCount = wordCount;
                    break;
                case "--tone":
                    request.Tone = args[i + 1];
                    break;
                case "--help":
                case "-h":
                    ShowUsage();
                    Environment.Exit(0);
                    break;
            }
        }

        return request;
    }

    /// <summary>
    /// Gets blog post request through interactive console input
    /// </summary>
    private static Task<BlogPostRequest> GetInteractiveInputAsync()
    {
        var request = new BlogPostRequest();

        Console.Write("Topic: ");
        request.Topic = Console.ReadLine() ?? string.Empty;

        Console.Write("Description (optional): ");
        request.Description = Console.ReadLine() ?? string.Empty;

        Console.Write($"Target Audience (default: {request.TargetAudience}): ");
        var audience = Console.ReadLine();
        if (!string.IsNullOrEmpty(audience))
            request.TargetAudience = audience;

        Console.Write($"Word Count (default: {request.WordCount}): ");
        var wordCountInput = Console.ReadLine();
        if (!string.IsNullOrEmpty(wordCountInput) && int.TryParse(wordCountInput, out int wordCount))
            request.WordCount = wordCount;

        Console.Write($"Tone (default: {request.Tone}): ");
        var tone = Console.ReadLine();
        if (!string.IsNullOrEmpty(tone))
            request.Tone = tone;

        return Task.FromResult(request);
    }

    /// <summary>
    /// Shows command line usage help
    /// </summary>
    private static void ShowUsage()
    {
        Console.WriteLine("Blog Post Generator Usage (Microsoft Agent Framework):");
        Console.WriteLine();
        Console.WriteLine("JSON File Input (Recommended):");
        Console.WriteLine("  dotnet run sample-request.json");
        Console.WriteLine("  dotnet run my-blog-request.json");
        Console.WriteLine();
        Console.WriteLine("Auto-discovery (looks for these files in order):");
        Console.WriteLine("  - blog-request.json");
        Console.WriteLine("  - request.json");
        Console.WriteLine("  - sample-request.json");
        Console.WriteLine();
        Console.WriteLine("Command Line Arguments:");
        Console.WriteLine("  --topic, -t        Blog post topic (required)");
        Console.WriteLine("  --description, -d  Blog post description");
        Console.WriteLine("  --audience, -a     Target audience");
        Console.WriteLine("  --wordcount, -w    Target word count");
        Console.WriteLine("  --tone            Writing tone");
        Console.WriteLine("  --help, -h        Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --topic \"API Security\" --description \"Best practices for securing APIs\" --audience \"Developers\" --wordcount 1500");
        Console.WriteLine("  dotnet run -t \"PowerShell Security\" -d \"Avoiding secrets in command history\" -a \"DevOps Engineers\"");
        Console.WriteLine();
        Console.WriteLine("Configuration File:");
        Console.WriteLine("  Add a 'BlogPostDefaults' section to appsettings.json");
        Console.WriteLine();
        Console.WriteLine("Interactive Mode:");
        Console.WriteLine("  Run without arguments to enter interactive mode");
    }
}