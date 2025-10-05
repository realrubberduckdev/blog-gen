using BlogPostGenerator.Framework;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BlogPostGenerator.Agents;

/// <summary>
/// SEO Agent responsible for optimizing content for search engines and generating metadata
/// </summary>
public class SEOAgent : BaseAgent, IAgent
{
    public string Name => Constants.Agents.SEO.Name;
    public string Description => Constants.Agents.SEO.Description;

    public SEOAgent(IChatClient chatClient, ILogger<SEOAgent> logger) 
        : base(chatClient, logger)
    {
    }

    /// <summary>
    /// Optimize blog post for SEO and create metadata
    /// </summary>
    /// <param name="content">Blog post content to optimize</param>
    /// <param name="topic">Main topic of the blog post</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SEO optimization data in JSON format</returns>
    public async Task<string> OptimizeForSEOAsync(
        string content,
        string topic,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"""
        You are an SEO specialist. Analyze the following blog post and create SEO optimization data:

        BLOG POST CONTENT:
        {content}

        MAIN TOPIC: {topic}

        Provide SEO optimization in JSON format with:
        1. Suggested title (SEO optimized, 50-60 characters)
        2. Meta description (150-160 characters)
        3. Primary keywords (5-7 keywords)
        4. Secondary keywords (3-5 keywords)
        5. Slug suggestion
        6. Social media descriptions (Twitter, LinkedIn, Facebook)
        7. Content improvements for SEO

        Return ONLY valid JSON without code blocks or explanations.
        """;

        _logger.LogInformation("Starting SEO optimization for topic: {Topic}", topic);
        return await ExecutePromptAsync(prompt, cancellationToken);
    }
}