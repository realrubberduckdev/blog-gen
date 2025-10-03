using BlogPostGenerator.Agents;
using BlogPostGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BlogPostGenerator.Services;

/// <summary>
/// Main orchestrator service using Microsoft Agent Framework that coordinates the blog post generation pipeline
/// </summary>
public class BlogPostOrchestrator
{
    private readonly ResearchAgent _researchAgent;
    private readonly ContentWriterAgent _contentWriterAgent;
    private readonly EditorAgent _editorAgent;
    private readonly MarkdownLinterAgent _markdownLinterAgent;
    private readonly SEOAgent _seoAgent;
    private readonly ILogger<BlogPostOrchestrator> _logger;

    public BlogPostOrchestrator(
        ResearchAgent researchAgent,
        ContentWriterAgent contentWriterAgent,
        EditorAgent editorAgent,
        MarkdownLinterAgent markdownLinterAgent,
        SEOAgent seoAgent,
        ILogger<BlogPostOrchestrator> logger)
    {
        _researchAgent = researchAgent;
        _contentWriterAgent = contentWriterAgent;
        _editorAgent = editorAgent;
        _markdownLinterAgent = markdownLinterAgent;
        _seoAgent = seoAgent;
        _logger = logger;
    }

    /// <summary>
    /// Generate a complete blog post using the Microsoft Agent Framework pipeline
    /// </summary>
    /// <param name="request">Blog post generation request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete blog post result with content and metadata</returns>
    public async Task<BlogPostResult> GenerateBlogPostAsync(
        BlogPostRequest request, 
        CancellationToken cancellationToken = default)
    {
        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Starting blog post generation pipeline for: {Topic}", request.Topic);

        try
        {
            // Step 1: Research and outline
            _logger.LogInformation("Step 1: Research and outline creation");
            var stepStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var outline = await _researchAgent.ResearchAndOutlineAsync(
                request.Topic, 
                request.Description, 
                request.TargetAudience,
                cancellationToken);
            stepStopwatch.Stop();
            _logger.LogInformation("Step 1 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Step 2: Content writing
            _logger.LogInformation("Step 2: Content writing");
            stepStopwatch.Restart();
            var content = await _contentWriterAgent.WriteContentAsync(
                outline, 
                request.Tone, 
                request.WordCount,
                cancellationToken);
            stepStopwatch.Stop();
            _logger.LogInformation("Step 2 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Step 3: Editing
            _logger.LogInformation("Step 3: Content editing");
            stepStopwatch.Restart();
            var editedContent = await _editorAgent.ReviewAndEditAsync(content, cancellationToken);
            stepStopwatch.Stop();
            _logger.LogInformation("Step 3 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Extract the edited content (remove editor notes for linting)
            var finalContent = ExtractEditedContent(editedContent);

            // Step 4: Markdown linting
            _logger.LogInformation("Step 4: Markdown validation");
            stepStopwatch.Restart();
            var lintingReport = await _markdownLinterAgent.ValidateMarkdownStructureAsync(
                finalContent, 
                cancellationToken);
            stepStopwatch.Stop();
            _logger.LogInformation("Step 4 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Step 5: SEO optimization
            _logger.LogInformation("Step 5: SEO optimization");
            stepStopwatch.Restart();
            var seoData = await _seoAgent.OptimizeForSEOAsync(
                finalContent, 
                request.Topic,
                cancellationToken);
            stepStopwatch.Stop();
            _logger.LogInformation("Step 5 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Parse SEO data
            var seoMetadata = ParseSeoMetadata(seoData);

            totalStopwatch.Stop();
            _logger.LogInformation("Blog post generation completed successfully! Total time: {TotalTime}",
                FormatElapsedTime(totalStopwatch));

            return new BlogPostResult
            {
                Title = seoMetadata.GetValueOrDefault("title", request.Topic),
                Content = finalContent,
                MetaDescription = seoMetadata.GetValueOrDefault("metaDescription", ""),
                Tags = ParseKeywords(seoMetadata.GetValueOrDefault("primaryKeywords", "")),
                Summary = seoMetadata.GetValueOrDefault("summary", "")
            };
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            _logger.LogError(ex, "Error occurred during blog post generation after {ElapsedTime}", 
                FormatElapsedTime(totalStopwatch));
            throw;
        }
    }

    private string ExtractEditedContent(string editedContent)
    {
        var contentStart = editedContent.IndexOf("EDITED CONTENT:", StringComparison.OrdinalIgnoreCase);
        if (contentStart == -1) return editedContent;

        var editorNotesStart = editedContent.IndexOf("EDITOR NOTES:", StringComparison.OrdinalIgnoreCase);
        if (editorNotesStart == -1) return editedContent[(contentStart + 15)..].Trim();

        return editedContent.Substring(contentStart + 15, editorNotesStart - contentStart - 15).Trim();
    }

    private string ExtractEditorNotes(string editedContent)
    {
        var notesStart = editedContent.IndexOf("EDITOR NOTES:", StringComparison.OrdinalIgnoreCase);
        return notesStart == -1 ? "" : editedContent[(notesStart + 13)..].Trim();
    }

    private Dictionary<string, string> ParseSeoMetadata(string seoData)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(seoData);
            var metadata = new Dictionary<string, string>();

            foreach (var property in jsonElement.EnumerateObject())
            {
                metadata[property.Name] = property.Value.GetString() ?? "";
            }

            return metadata;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse SEO metadata, using fallback values");
            return new Dictionary<string, string>();
        }
    }

    private List<string> ParseKeywords(string keywordsString)
    {
        if (string.IsNullOrEmpty(keywordsString)) return new List<string>();

        return keywordsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .ToList();
    }

    private string GenerateSlug(string topic)
    {
        return topic.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and");
    }

    private string FormatElapsedTime(System.Diagnostics.Stopwatch stopwatch)
    {
        var elapsed = stopwatch.Elapsed;
        return $"{elapsed.Hours:D2}h:{elapsed.Minutes:D2}m:{elapsed.Seconds:D2}s:{elapsed.Milliseconds:D3}ms";
    }
}