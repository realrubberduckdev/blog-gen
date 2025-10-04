using System.Text.Json;
using BlogPostGenerator.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlogPostGenerator.Services;

/// <summary>
/// Service that encapsulates the blog generation workflow using Microsoft Agent Framework
/// </summary>
public class BlogGenerationService
{
    private readonly IChatClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlogGenerationService> _logger;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public BlogGenerationService(IChatClient client, IConfiguration configuration, ILogger<BlogGenerationService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync(string[] args)
    {
        Console.WriteLine("=== Multi-Agent Blog Generation System ===\n");

        // Get blog post request from multiple sources
        var blogRequest = await GetBlogPostRequestAsync(args);

        Console.WriteLine($"\n🚀 Starting blog generation for: '{blogRequest.Topic}'");
        Console.WriteLine($"📝 Description: {blogRequest.Description}\n");

        // Create specialized agents (use the IChatClient directly with system messages)
        var researchAgent = _client;
        var writerAgent = _client;
        var editorAgent = _client;
        var linterAgent = _client;
        var seoAgent = _client;

        try
        {
            // Step 1: Research
            Console.WriteLine("📚 Step 1: Researching topic...");
            var researchMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are a research specialist. Your role is to:
                1. Research topics thoroughly and create comprehensive outlines
                2. Gather key facts, statistics, and insights
                3. Identify target audience needs and pain points
                4. Suggest compelling title options
                5. Structure content logically with clear sections
                
                Always provide structured, well-organized research that serves as a foundation for high-quality blog content.
                """),
                new(ChatRole.User, 
                    $"Research the topic '{blogRequest.Topic}' with focus on: {blogRequest.Description}. " +
                    $"Target audience: {blogRequest.TargetAudience}. Create a comprehensive outline.")
            };

            var researchResponse = await researchAgent.CompleteAsync(researchMessages);
            var outline = researchResponse.Message.Text ?? "";
            Console.WriteLine($"✅ Research completed. Outline length: {outline.Length} characters\n");

            // Step 2: Write content
            Console.WriteLine("✍️ Step 2: Writing content...");
            var writeMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are an expert content writer. Your role is to:
                1. Transform research outlines into engaging blog posts
                2. Write clear, compelling, and valuable content
                3. Use appropriate tone and style for the target audience
                4. Include engaging introductions and strong conclusions
                5. Format content in clean markdown
                
                Focus on creating content that provides real value to readers and maintains engagement throughout.
                """),
                new(ChatRole.User,
                    $"Write a {blogRequest.WordCount}-word blog post based on this outline: {outline}. " +
                    $"Use a {blogRequest.Tone} tone and write in markdown format.")
            };

            var writeResponse = await writerAgent.CompleteAsync(writeMessages);
            var content = writeResponse.Message.Text ?? "";
            Console.WriteLine($"✅ Content written. Length: {content.Length} characters\n");

            // Step 3: Edit content
            Console.WriteLine("📝 Step 3: Editing content...");
            var editMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are a professional editor. Your role is to:
                1. Review content for grammar, spelling, and clarity
                2. Improve readability and flow
                3. Ensure consistency in tone and style
                4. Enhance structure and organization
                5. Maintain the original intent while improving quality
                
                Provide polished, publication-ready content that maintains the author's voice while ensuring professional quality.
                """),
                new(ChatRole.User,
                    $"Review and edit this blog post for quality and consistency: {content}")
            };

            var editResponse = await editorAgent.CompleteAsync(editMessages);
            var editedContent = editResponse.Message.Text ?? "";
            Console.WriteLine($"✅ Content edited. Length: {editedContent.Length} characters\n");

            // Step 4: Lint markdown
            Console.WriteLine("🔧 Step 4: Linting markdown...");
            var lintMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are a markdown linter and formatter. Your role is to:
                1. Fix markdown formatting issues and inconsistencies
                2. Ensure proper header hierarchy and structure
                3. Format code blocks, lists, and links correctly
                4. Remove unnecessary whitespace and formatting errors
                5. Ensure the content follows markdown best practices
                
                Return clean, properly formatted markdown that passes linting standards.
                """),
                new(ChatRole.User,
                    $"Lint and fix markdown formatting issues in this content: {editedContent}")
            };

            var lintResponse = await linterAgent.CompleteAsync(lintMessages);
            var lintedContent = lintResponse.Message.Text ?? "";
            Console.WriteLine($"✅ Markdown linted. Length: {lintedContent.Length} characters\n");

            // Step 5: SEO optimization
            Console.WriteLine("🎯 Step 5: SEO optimization...");
            var seoMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are an SEO specialist. Your role is to:
                1. Analyze content for SEO optimization opportunities
                2. Generate compelling meta descriptions and titles
                3. Identify primary and secondary keywords
                4. Create social media descriptions
                5. Provide SEO-friendly content recommendations
                
                Return structured JSON data with SEO metadata and optimization suggestions.
                """),
                new(ChatRole.User,
                    $"Optimize this blog post for SEO and create metadata: {lintedContent}. Topic: {blogRequest.Topic}")
            };

            var seoResponse = await seoAgent.CompleteAsync(seoMessages);
            var seoData = seoResponse.Message.Text ?? "";
            Console.WriteLine($"✅ SEO optimization completed.\n");

            // Create final result
            var result = new BlogPostResult
            {
                Title = blogRequest.Topic,
                Content = lintedContent,
                MetaDescription = ExtractMetaDescription(seoData),
                Summary = ExtractSummary(lintedContent),
                Tags = ExtractTags(seoData)
            };

            // Save to file
            var outputPath = "generated-blog-post.md";
            await File.WriteAllTextAsync(outputPath, result.Content);
            
            var seoOutputPath = "blog-seo-data.json";
            await File.WriteAllTextAsync(seoOutputPath, seoData);

            Console.WriteLine($"🎉 Blog generation completed!");
            Console.WriteLine($"📄 Content saved to: {outputPath}");
            Console.WriteLine($"🎯 SEO data saved to: {seoOutputPath}");
            Console.WriteLine($"📊 Final content length: {result.Content.Length} characters");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during blog generation");
            Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Extracts meta description from SEO data JSON
    /// </summary>
    private string ExtractMetaDescription(string seoData)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(seoData);
            if (jsonDoc.RootElement.TryGetProperty("metaDescription", out var metaDesc))
            {
                return metaDesc.GetString() ?? "";
            }
            if (jsonDoc.RootElement.TryGetProperty("meta_description", out var metaDesc2))
            {
                return metaDesc2.GetString() ?? "";
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }
        return "";
    }

    /// <summary>
    /// Extracts summary from content (first paragraph or intro)
    /// </summary>
    private string ExtractSummary(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('#') && trimmed.Length > 50)
            {
                return trimmed.Length > 200 ? trimmed[..200] + "..." : trimmed;
            }
        }
        return "";
    }

    /// <summary>
    /// Extracts tags from SEO data JSON
    /// </summary>
    private List<string> ExtractTags(string seoData)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(seoData);
            if (jsonDoc.RootElement.TryGetProperty("keywords", out var keywords))
            {
                var tags = new List<string>();
                if (keywords.ValueKind == JsonValueKind.Array)
                {
                    foreach (var keyword in keywords.EnumerateArray())
                    {
                        var tag = keyword.GetString();
                        if (!string.IsNullOrEmpty(tag))
                            tags.Add(tag);
                    }
                }
                return tags;
            }
            if (jsonDoc.RootElement.TryGetProperty("primaryKeywords", out var primaryKeywords))
            {
                var tags = new List<string>();
                if (primaryKeywords.ValueKind == JsonValueKind.Array)
                {
                    foreach (var keyword in primaryKeywords.EnumerateArray())
                    {
                        var tag = keyword.GetString();
                        if (!string.IsNullOrEmpty(tag))
                            tags.Add(tag);
                    }
                }
                return tags;
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }
        return new List<string>();
    }

    /// <summary>
    /// Gets blog post request from multiple sources: JSON file, command line args, config file, or interactive input
    /// </summary>
    private async Task<BlogPostRequest> GetBlogPostRequestAsync(string[] args)
    {
        // Try JSON file input first
        if (args.Length > 0)
        {
            // Check if first argument is a JSON file
            if (args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]))
            {
                return await LoadFromJsonFileAsync(args[0]);
            }
            
            // Try parsing command line arguments
            var request = ParseCommandLineArgs(args);
            if (!string.IsNullOrEmpty(request.Topic))
                return request;
        }

        // Try to find JSON files in current directory
        var jsonFiles = new[] { "blog-request.json", "request.json", "sample-request.json" };
        foreach (var jsonFile in jsonFiles)
        {
            if (File.Exists(jsonFile))
            {
                Console.WriteLine($"Found {jsonFile}, using it for blog generation...");
                return await LoadFromJsonFileAsync(jsonFile);
            }
        }

        // Try configuration file
        var configRequest = _configuration.GetSection("BlogRequest").Get<BlogPostRequest>();
        if (configRequest != null && !string.IsNullOrEmpty(configRequest.Topic))
        {
            Console.WriteLine("Using blog request from configuration file...");
            return configRequest;
        }

        // Fall back to interactive input
        Console.WriteLine("No blog request found. Let's create one interactively...");
        return await GetInteractiveInputAsync();
    }

    /// <summary>
    /// Loads BlogPostRequest from a JSON file with error handling and validation
    /// </summary>
    private async Task<BlogPostRequest> LoadFromJsonFileAsync(string filePath)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath);
            var request = JsonSerializer.Deserialize<BlogPostRequest>(jsonContent, s_jsonOptions);
            
            if (request == null)
            {
                throw new InvalidOperationException("Failed to deserialize blog request from JSON");
            }

            if (string.IsNullOrEmpty(request.Topic))
            {
                throw new InvalidOperationException("Blog request must have a topic");
            }

            Console.WriteLine($"Loaded blog request from {filePath}");
            return request;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing JSON file {filePath}: {ex.Message}");
            Console.WriteLine("Please check the JSON format and try again.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading blog request from {filePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses command line arguments into BlogPostRequest
    /// </summary>
    private BlogPostRequest ParseCommandLineArgs(string[] args)
    {
        var request = new BlogPostRequest();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--topic":
                case "-t":
                    if (i + 1 < args.Length) request.Topic = args[++i];
                    break;
                case "--description":
                case "-d":
                    if (i + 1 < args.Length) request.Description = args[++i];
                    break;
                case "--audience":
                case "-a":
                    if (i + 1 < args.Length) request.TargetAudience = args[++i];
                    break;
                case "--wordcount":
                case "-w":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int words))
                        request.WordCount = words;
                    break;
                case "--tone":
                    if (i + 1 < args.Length) request.Tone = args[++i];
                    break;
            }
        }

        return request;
    }

    /// <summary>
    /// Gets blog post request through interactive console input
    /// </summary>
    private Task<BlogPostRequest> GetInteractiveInputAsync()
    {
        Console.Write("Enter blog topic: ");
        var topic = Console.ReadLine() ?? "The Future of AI";
        
        Console.Write("Enter description (optional): ");
        var description = Console.ReadLine() ?? "An exploration of artificial intelligence trends";
        
        Console.Write("Enter target audience (default: General): ");
        var audience = Console.ReadLine();
        if (string.IsNullOrEmpty(audience)) audience = "General";
        
        Console.Write("Enter word count (default: 800): ");
        var wordCountInput = Console.ReadLine();
        int wordCount = 800;
        if (!string.IsNullOrEmpty(wordCountInput) && int.TryParse(wordCountInput, out int parsed))
            wordCount = parsed;
        
        Console.Write("Enter tone (default: Professional): ");
        var tone = Console.ReadLine();
        if (string.IsNullOrEmpty(tone)) tone = "Professional";

        return Task.FromResult(new BlogPostRequest
        {
            Topic = topic,
            Description = description,
            TargetAudience = audience,
            WordCount = wordCount,
            Tone = tone
        });
    }
}