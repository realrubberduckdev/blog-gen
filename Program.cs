using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Text.Json;

namespace BlogPostGenerator;

public class BlogPostRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = "General";
    public int WordCount { get; set; } = 800;
    public string Tone { get; set; } = "Professional";
}

public class BlogPostResult
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string MetaDescription { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

// Research Agent - Gathers information and creates outline
public class ResearchAgent
{
    private readonly Kernel _kernel;

    public ResearchAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction, Description("Research topic and create blog post outline")]
    public async Task<string> ResearchAndOutlineAsync(
        [Description("The topic to research")] string topic,
        [Description("Brief description of what to focus on")] string description,
        [Description("Target audience")] string audience = "General")
    {
        var prompt = $"""
        You are a research specialist. Research the topic "{topic}" with focus on: {description}
        
        Target audience: {audience}
        
        Create a comprehensive outline for a blog post including:
        1. Compelling title suggestions (3 options)
        2. Main sections with bullet points
        3. Key facts and statistics to include
        4. Potential quotes or expert insights
        5. Relevant examples or case studies
        
        Format your response as structured text that can be easily parsed.
        """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
    }
}

// Content Writer Agent - Creates the main blog content
public class ContentWriterAgent
{
    private readonly Kernel _kernel;

    public ContentWriterAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction, Description("Write blog post content based on research outline")]
    public async Task<string> WriteContentAsync(
        [Description("Research outline and structure")] string outline,
        [Description("Writing tone (Professional, Casual, Technical, etc.)")] string tone = "Professional",
        [Description("Target word count")] int wordCount = 800)
    {
        var prompt = $"""
        You are an expert content writer. Using the following research outline, write a compelling blog post:
        
        OUTLINE:
        {outline}
        
        REQUIREMENTS:
        - Tone: {tone}
        - Target word count: approximately {wordCount} words
        - Include an engaging introduction
        - Use clear headings and subheadings
        - Add relevant examples and explanations
        - Include a strong conclusion with call-to-action
        - Write in markdown format
        
        Focus on creating valuable, engaging content that provides real insights to readers.
        """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
    }
}

// SEO Agent - Optimizes content for search engines
public class SEOAgent
{
    private readonly Kernel _kernel;

    public SEOAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction, Description("Optimize blog post for SEO and create metadata")]
    public async Task<string> OptimizeForSEOAsync(
        [Description("Blog post content")] string content,
        [Description("Main topic")] string topic)
    {
        var prompt = $"""
        You are an SEO specialist. Analyze the following blog post and provide SEO optimization:
        
        CONTENT:
        {content}
        
        MAIN TOPIC: {topic}
        
        Provide the following in JSON format:
        1. SEO-optimized title (60 characters or less)
        2. Meta description (150-160 characters)
        3. Suggested tags/keywords (5-10 relevant tags)
        4. Brief summary (2-3 sentences)
        5. Any suggestions for content improvements
        
        Format as valid JSON with keys: title, metaDescription, tags, summary, suggestions
        """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
    }
}

// Editor Agent - Reviews and polishes the content
public class EditorAgent
{
    private readonly Kernel _kernel;

    public EditorAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction, Description("Review and edit blog post for quality and consistency")]
    public async Task<string> ReviewAndEditAsync(
        [Description("Blog post content to review")] string content)
    {
        var prompt = $"""
        You are a professional editor. Review the following blog post for:
        
        CONTENT TO REVIEW:
        {content}
        
        Check for:
        1. Grammar and spelling errors
        2. Clarity and readability
        3. Logical flow and structure
        4. Consistency in tone and style
        5. Factual accuracy (flag any questionable claims)
        
        Return the improved version of the blog post, maintaining the original structure but fixing any issues you find.
        Also provide a brief note about changes made.
        
        Format: 
        EDITED CONTENT:
        [improved content here]
        
        EDITOR NOTES:
        [brief summary of changes made]
        """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
    }
}

// Main orchestrator class
public class BlogPostOrchestrator
{
    private readonly ResearchAgent _researchAgent;
    private readonly ContentWriterAgent _writerAgent;
    private readonly SEOAgent _seoAgent;
    private readonly EditorAgent _editorAgent;
    private readonly ILogger<BlogPostOrchestrator> _logger;

    public BlogPostOrchestrator(
        ResearchAgent researchAgent,
        ContentWriterAgent writerAgent,
        SEOAgent seoAgent,
        EditorAgent editorAgent,
        ILogger<BlogPostOrchestrator> logger)
    {
        _researchAgent = researchAgent;
        _writerAgent = writerAgent;
        _seoAgent = seoAgent;
        _editorAgent = editorAgent;
        _logger = logger;
    }

    public async Task<BlogPostResult> GenerateBlogPostAsync(BlogPostRequest request)
    {
        _logger.LogInformation("Starting blog post generation for topic: {Topic}", request.Topic);

        try
        {
            // Step 1: Research and create outline
            _logger.LogInformation("Step 1: Researching topic and creating outline...");
            var outline = await _researchAgent.ResearchAndOutlineAsync(
                request.Topic, 
                request.Description, 
                request.TargetAudience);

            // Step 2: Write initial content
            _logger.LogInformation("Step 2: Writing blog post content...");
            var initialContent = await _writerAgent.WriteContentAsync(
                outline, 
                request.Tone, 
                request.WordCount);

            // Step 3: Edit and polish content
            _logger.LogInformation("Step 3: Editing and polishing content...");
            var editedContent = await _editorAgent.ReviewAndEditAsync(initialContent);
            
            // Extract the edited content (remove editor notes)
            var contentParts = editedContent.Split(new[] { "EDITOR NOTES:" }, StringSplitOptions.None);
            var finalContent = contentParts[0].Replace("EDITED CONTENT:", "").Trim();

            // Step 4: SEO optimization
            _logger.LogInformation("Step 4: Optimizing for SEO...");
            var seoData = await _seoAgent.OptimizeForSEOAsync(finalContent, request.Topic);

            // Parse SEO JSON response
            var seoResult = ParseSEOResponse(seoData);

            _logger.LogInformation("Blog post generation completed successfully!");

            return new BlogPostResult
            {
                Title = seoResult.Title,
                Content = finalContent,
                Tags = seoResult.Tags,
                MetaDescription = seoResult.MetaDescription,
                Summary = seoResult.Summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating blog post");
            throw;
        }
    }

    private (string Title, List<string> Tags, string MetaDescription, string Summary) ParseSEOResponse(string seoJson)
    {
        try
        {
            // Clean up the JSON response if it contains extra text
            var jsonStart = seoJson.IndexOf('{');
            var jsonEnd = seoJson.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                seoJson = seoJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            var seoData = JsonSerializer.Deserialize<JsonElement>(seoJson);
            
            return (
                Title: seoData.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                Tags: seoData.TryGetProperty("tags", out var tags) ? 
                      tags.EnumerateArray().Select(t => t.GetString() ?? "").ToList() : new List<string>(),
                MetaDescription: seoData.TryGetProperty("metaDescription", out var meta) ? meta.GetString() ?? "" : "",
                Summary: seoData.TryGetProperty("summary", out var summary) ? summary.GetString() ?? "" : ""
            );
        }
        catch (JsonException)
        {
            // Fallback if JSON parsing fails
            return ("Generated Blog Post", new List<string>(), "", "");
        }
    }
}

// Program entry point and configuration
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
        
        // Use OpenAI API key from user secrets
        builder.AddAzureOpenAIChatCompletion(
            "gpt-4o-mini",
            "https://dp-openai1.openai.azure.com/openai/deployments/gpt-4o-mini/chat/completions?api-version=2025-01-01-preview",
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