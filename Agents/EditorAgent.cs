using BlogPostGenerator.Framework;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BlogPostGenerator.Agents;

/// <summary>
/// Editor Agent responsible for reviewing and polishing blog content for quality and consistency
/// </summary>
public class EditorAgent : BaseAgent, IAgent
{
    public string Name => "Editor Agent";
    public string Description => "Reviews and polishes content for quality";

    public EditorAgent(IChatClient chatClient, ILogger<EditorAgent> logger) 
        : base(chatClient, logger)
    {
    }

    /// <summary>
    /// Review and edit blog post for quality and consistency
    /// </summary>
    /// <param name="content">Blog post content to review</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Improved blog post content with editor notes</returns>
    public async Task<string> ReviewAndEditAsync(
        string content,
        CancellationToken cancellationToken = default)
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

        _logger.LogInformation("Starting content review and editing");
        return await ExecutePromptAsync(prompt, cancellationToken);
    }
}