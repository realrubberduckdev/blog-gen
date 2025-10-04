using System.Diagnostics;
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
        var totalStopwatch = Stopwatch.StartNew();

        _logger.LogInformation("=== Multi-Agent Blog Generation System ===");

        // Get blog post request from multiple sources
        var blogRequest = await GetBlogPostRequestAsync(args);

        _logger.LogInformation("🚀 Starting blog generation for: '{Topic}'", blogRequest.Topic);
        _logger.LogInformation("📝 Description: {Description}", blogRequest.Description);

        // Create specialized agents (use the IChatClient directly with system messages)
        var researchAgent = _client;
        var writerAgent = _client;
        var editorAgent = _client;
        var linterAgent = _client;
        var seoAgent = _client;

        try
        {
            // Step 1: Research
            var researchStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("📚 Step 1: Researching topic...");
            var researchMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are a research specialist.

                Create a comprehensive outline for a blog post including:
                1. Compelling title suggestions (3 options)
                2. Main sections with bullet points
                3. Key facts and statistics to include
                4. Potential quotes or expert insights
                5. Relevant examples or case studies

                Format your response as structured text that can be easily parsed.
                """),
                new(ChatRole.User,
                    $"Research the topic '{blogRequest.Topic}' with focus on: {blogRequest.Description}. " +
                    $"Target audience: {blogRequest.TargetAudience}. Create a comprehensive outline.")
            };

            var researchResponse = await researchAgent.CompleteAsync(researchMessages);
            researchStopwatch.Stop();
            var outline = researchResponse.Message.Text ?? "";
            _logger.LogInformation(
                "✅ Research agent completed in {ElapsedTime}. Outline length: {length} characters\n",
                FormatElapsedTime(researchStopwatch.Elapsed),
                outline.Length);

            // Step 2: Write content
            var writerStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("✍️ Step 2: Writing content...");
            var writeMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are an expert content writer. Using the following research outline, write a compelling blog post.

                REQUIREMENTS:
                - Include an engaging introduction
                - Use clear headings and subheadings
                - Add relevant examples and explanations
                - Include a strong conclusion with call-to-action
                - Write in markdown format
                - Do NOT wrap the output in markdown code blocks (```markdown or ```)

                Focus on creating valuable, engaging content that provides real insights to readers.
                """),
                new(ChatRole.User,
                    $"Write a {blogRequest.WordCount}-word blog post based on this outline: {outline}. " +
                    $"Use a {blogRequest.Tone} tone and write in markdown format.")
            };

            var writeResponse = await writerAgent.CompleteAsync(writeMessages);
            writerStopwatch.Stop();
            var content = writeResponse.Message.Text ?? "";
            _logger.LogInformation(
                "✅ Content written in {ElapsedTime}. Content length: {length} characters\n",
                FormatElapsedTime(writerStopwatch.Elapsed),
                content.Length);

            // Step 3: Edit content
            var editorStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("📝 Step 3: Editing content...");
            var editMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are a professional editor. Review the blog post content provided by user.

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
                """),
                new(ChatRole.User,
                    $"Review and edit this blog post for quality and consistency: {content}")
            };

            var editResponse = await editorAgent.CompleteAsync(editMessages);
            editorStopwatch.Stop();
            var editedContent = editResponse.Message.Text ?? "";
            _logger.LogInformation(
                "✅ Content edited in {ElapsedTime}. Content length: {length} characters\n",
                FormatElapsedTime(editorStopwatch.Elapsed),
                editedContent.Length);

            // Step 4: Lint markdown
            var linterStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("🔧 Step 4: Linting markdown...");
            var lintMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are a markdown linter and formatter. Review the markdown content provided by the user and fix any linting issues.

                Check for and fix these markdown linting issues:
                1. **Headers**: Ensure proper header hierarchy (h1 → h2 → h3, no skipping levels)
                2. **Spacing**: Add proper blank lines around headers, code blocks, and lists
                3. **Lists**: Consistent indentation and formatting for ordered/unordered lists
                4. **Links**: Proper link formatting [text](url) and reference-style links
                5. **Code blocks**: Proper fencing with language specification where appropriate
                6. **Emphasis**: Consistent use of *italic* and **bold** formatting
                7. **Line length**: Break overly long lines at natural points (aim for ~80-100 characters)
                8. **Trailing spaces**: Remove unnecessary trailing whitespace
                9. **Empty lines**: Remove multiple consecutive empty lines
                10. **Special characters**: Proper escaping of markdown special characters in text

                Additional formatting improvements:
                - Ensure code snippets have proper syntax highlighting language tags
                - Make sure table formatting is consistent and aligned
                - Verify that blockquotes use proper > formatting
                - Check that horizontal rules use consistent syntax (---)
                - Return ONLY the corrected markdown content. Do not include explanations or notes about what was changed.
                - Do NOT wrap the output in markdown code blocks (```markdown or ```).
                - The output should be clean, properly formatted markdown that passes standard linting rules.
                """),
                new(ChatRole.User,
                    $"Lint and fix markdown formatting issues in this content: {editedContent}")
            };

            var lintResponse = await linterAgent.CompleteAsync(lintMessages);
            linterStopwatch.Stop();
            var lintedContent = lintResponse.Message.Text ?? "";
            _logger.LogInformation(
                "✅ Markdown linted in {ElapsedTime}. Content length: {length} characters\n",
                FormatElapsedTime(linterStopwatch.Elapsed),
                lintedContent.Length);

            // Step 5: SEO optimization
            var seoStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("🎯 Step 5: SEO optimization...");
            var seoMessages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                You are an SEO specialist. Analyze the blog post provided by the user and provide SEO optimization.

                Provide the following in JSON format:
                - SEO-optimized title (60 characters or less)
                - Meta description (150-160 characters)
                - Suggested tags/keywords (5-10 relevant tags)
                - Brief summary (2-3 sentences)
                - Any suggestions for content improvements
                - Do NOT wrap the output in markdown code blocks (```markdown or ```json or ```).

                Format as valid JSON with keys: title, metaDescription, tags, summary, suggestions
                """),
                new(ChatRole.User,
                    $"Optimize this blog post for SEO and create metadata: {lintedContent}. Topic: {blogRequest.Topic}")
            };

            var seoResponse = await seoAgent.CompleteAsync(seoMessages);
            seoStopwatch.Stop();
            var seoData = seoResponse.Message.Text ?? "";
            _logger.LogInformation(
                "✅ SEO optimization completed in {ElapsedTime}. Content length: {length} characters\n",
                FormatElapsedTime(seoStopwatch.Elapsed),
                seoData.Length);

            // Create final result
            var result = new BlogPostResult
            {
                Title = blogRequest.Topic,
                Content = lintedContent,
                MetaDescription = ExtractMetaDescription(seoData),
                Summary = ExtractSummary(lintedContent),
                Tags = ExtractTags(seoData)
            };

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

            // Save to file
            var fileName = $"{DateTime.Now:yyyy-MM-dd}-{result.Title.Replace(" ", "-").Replace(":", "").Replace("?", "").Replace("/", "-").Replace("\\", "-").ToLower()}.md";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            await File.WriteAllTextAsync(filePath, markdownContent);

            var seoOutputPath = "blog-seo-data.json";
            await File.WriteAllTextAsync(seoOutputPath, seoData);

            totalStopwatch.Stop();

            // Log timing summary
            var totalTimeMs = totalStopwatch.ElapsedMilliseconds;
            _logger.LogInformation("🎉 Blog generation completed!");
            _logger.LogInformation("📄 Content saved to: {OutputPath}", filePath);
            _logger.LogInformation("🎯 SEO data saved to: {SeoOutputPath}", seoOutputPath);
            _logger.LogInformation("📊 Final content length: {ContentLength} characters", result.Content.Length);
            _logger.LogInformation("⏱️ Total time: {TotalTime}", FormatElapsedTime(totalStopwatch.Elapsed));
            _logger.LogInformation("   └─ Research: {ResearchTime}", FormatElapsedTime(researchStopwatch.Elapsed));
            _logger.LogInformation("   └─ Writing: {WritingTime}", FormatElapsedTime(writerStopwatch.Elapsed));
            _logger.LogInformation("   └─ Editing: {EditingTime}", FormatElapsedTime(editorStopwatch.Elapsed));
            _logger.LogInformation("   └─ Linting: {LintingTime}", FormatElapsedTime(linterStopwatch.Elapsed));
            _logger.LogInformation("   └─ SEO: {SeoTime}", FormatElapsedTime(seoStopwatch.Elapsed));
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            _logger.LogError(ex, "Error during blog generation after {ElapsedTime} ms ({ElapsedTimeFormatted})",
                totalStopwatch.ElapsedMilliseconds, FormatElapsedTime(totalStopwatch.Elapsed));
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
            if (jsonDoc.RootElement.TryGetProperty("tags", out var tagsJsonArray))
            {
                var tags = new List<string>();
                if (tagsJsonArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tag in tagsJsonArray.EnumerateArray())
                    {
                        var tagValue = tag.GetString();
                        if (!string.IsNullOrEmpty(tagValue))
                            tags.Add(tagValue);
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
                _logger.LogInformation("Found {JsonFile}, using it for blog generation...", jsonFile);
                return await LoadFromJsonFileAsync(jsonFile);
            }
        }

        // Try configuration file
        var configRequest = _configuration.GetSection("BlogRequest").Get<BlogPostRequest>();
        if (configRequest != null && !string.IsNullOrEmpty(configRequest.Topic))
        {
            _logger.LogInformation("Using blog request from configuration file...");
            return configRequest;
        }

        // Fall back to interactive input
        _logger.LogInformation("No blog request found. Let's create one interactively...");
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

            _logger.LogInformation("Loaded blog request from {FilePath}", filePath);
            return request;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing JSON file {FilePath}: {ErrorMessage}", filePath, ex.Message);
            _logger.LogError("Please check the JSON format and try again.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading blog request from {FilePath}: {ErrorMessage}", filePath, ex.Message);
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

    /// <summary>
    /// Formats elapsed time as HH:mm:ss.fff with appropriate units
    /// </summary>
    private string FormatElapsedTime(TimeSpan elapsed)
        => $"{elapsed.Hours:D2}h:{elapsed.Minutes:D2}m:{elapsed.Seconds:D2}s:{elapsed.Milliseconds:D3}ms";
}