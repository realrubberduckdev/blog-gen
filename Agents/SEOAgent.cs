using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace BlogPostGenerator.Agents;

/// <summary>
/// SEO Agent responsible for optimizing content for search engines and generating metadata
/// </summary>
public class SEOAgent
{
    private readonly Kernel _kernel;

    public SEOAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Optimize blog post for SEO and create metadata
    /// </summary>
    /// <param name="content">Blog post content to optimize</param>
    /// <param name="topic">Main topic of the blog post</param>
    /// <returns>SEO optimization data in JSON format</returns>
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