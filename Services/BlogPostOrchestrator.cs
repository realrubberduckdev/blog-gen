using BlogPostGenerator.Agents;
using BlogPostGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BlogPostGenerator.Services;

/// <summary>
/// Main orchestrator service that coordinates the blog post generation pipeline
/// </summary>
public class BlogPostOrchestrator
{
    private readonly ResearchAgent _researchAgent;
    private readonly ContentWriterAgent _writerAgent;
    private readonly SEOAgent _seoAgent;
    private readonly EditorAgent _editorAgent;
    private readonly MarkdownLinterAgent _markdownLinterAgent;
    private readonly ILogger<BlogPostOrchestrator> _logger;

    public BlogPostOrchestrator(
        ResearchAgent researchAgent,
        ContentWriterAgent writerAgent,
        SEOAgent seoAgent,
        EditorAgent editorAgent,
        MarkdownLinterAgent markdownLinterAgent,
        ILogger<BlogPostOrchestrator> logger)
    {
        _researchAgent = researchAgent;
        _writerAgent = writerAgent;
        _seoAgent = seoAgent;
        _editorAgent = editorAgent;
        _markdownLinterAgent = markdownLinterAgent;
        _logger = logger;
    }    /// <summary>
         /// Generate a complete blog post using the 5-stage pipeline
         /// </summary>
         /// <param name="request">Blog post generation request parameters</param>
         /// <returns>Complete blog post result with content and metadata</returns>
    public async Task<BlogPostResult> GenerateBlogPostAsync(BlogPostRequest request)
    {
        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Starting blog post generation for topic: {Topic}", request.Topic);

        try
        {
            // Step 1: Research and create outline
            _logger.LogInformation("Step 1: Researching topic and creating outline...");
            var stepStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var outline = await _researchAgent.ResearchAndOutlineAsync(
                request.Topic,
                request.Description,
                request.TargetAudience); stepStopwatch.Stop();
            _logger.LogInformation("Step 1 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Step 2: Write initial content
            _logger.LogInformation("Step 2: Writing blog post content...");
            stepStopwatch.Restart();
            var initialContent = await _writerAgent.WriteContentAsync(
                outline,
                request.Tone,
                request.WordCount); stepStopwatch.Stop();
            _logger.LogInformation("Step 2 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Step 3: Edit and polish content
            _logger.LogInformation("Step 3: Editing and polishing content...");
            stepStopwatch.Restart();
            var editedContent = await _editorAgent.ReviewAndEditAsync(initialContent); stepStopwatch.Stop();
            _logger.LogInformation("Step 3 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Extract the edited content (remove editor notes)
            var contentParts = editedContent.Split(new[] { "EDITOR NOTES:" }, StringSplitOptions.None);
            var processedContent = contentParts[0].Replace("EDITED CONTENT:", "").Trim();

            // Step 4: Markdown linting and formatting
            _logger.LogInformation("Step 4: Linting and formatting markdown...");
            stepStopwatch.Restart();
            var finalContent = await _markdownLinterAgent.LintAndFixMarkdownAsync(processedContent); stepStopwatch.Stop();
            _logger.LogInformation("Step 4 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Step 5: SEO optimization
            _logger.LogInformation("Step 5: Optimizing for SEO...");
            stepStopwatch.Restart();
            var seoData = await _seoAgent.OptimizeForSEOAsync(finalContent, request.Topic); stepStopwatch.Stop();
            _logger.LogInformation("Step 5 completed in {ElapsedTime}", FormatElapsedTime(stepStopwatch));

            // Parse SEO JSON response
            var seoResult = ParseSEOResponse(seoData); totalStopwatch.Stop();
            _logger.LogInformation("Blog post generation completed successfully! Total time: {TotalTime}",
                FormatElapsedTime(totalStopwatch));

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
            totalStopwatch.Stop();
            _logger.LogError(ex, "Error generating blog post after {ElapsedTime}", FormatElapsedTime(totalStopwatch));
            throw;
        }
    }    /// <summary>
         /// Format elapsed time with units (e.g., "01h:23m:45s:678ms")
         /// </summary>
         /// <param name="stopwatch">The stopwatch to format elapsed time for</param>
         /// <returns>Formatted time string with units for clarity</returns>
    private string FormatElapsedTime(System.Diagnostics.Stopwatch stopwatch)
    {
        var elapsed = stopwatch.Elapsed;
        return $"{elapsed.Hours:D2}h:{elapsed.Minutes:D2}m:{elapsed.Seconds:D2}s:{elapsed.Milliseconds:D3}ms";
    }

    /// <summary>
    /// Parse SEO response JSON with fallback handling for malformed JSON
    /// </summary>
    /// <param name="seoJson">JSON response from SEO agent</param>
    /// <returns>Parsed SEO data with fallback values</returns>
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