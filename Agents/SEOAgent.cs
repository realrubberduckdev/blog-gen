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

        Provide the following in JSON format:
        - SEO-optimized title (60 characters or less)
        - Meta description (150-160 characters)
        - Suggested tags/keywords (5-10 relevant tags)
        - Brief summary (2-3 sentences)
        - Any suggestions for content improvements
        - Do NOT wrap the output in markdown code blocks (```markdown or ```json or ```).

        Format as valid JSON with keys: title, metaDescription, tags, summary, suggestions
        """;

        _logger.LogInformation("Starting SEO optimization for topic: {Topic}", topic);
        return await ExecutePromptAsync(prompt, cancellationToken);
    }
}