using BlogPostGenerator.Framework;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BlogPostGenerator.Agents;

/// <summary>
/// Research Agent responsible for gathering information and creating blog post outlines
/// </summary>
public class ResearchAgent : BaseAgent, IAgent
{
    public string Name => Constants.Agents.Research.Name;
    public string Description => Constants.Agents.Research.Description;

    public ResearchAgent(IChatClient chatClient, ILogger<ResearchAgent> logger) 
        : base(chatClient, logger)
    {
    }

    /// <summary>
    /// Research a topic and create a comprehensive blog post outline
    /// </summary>
    /// <param name="topic">The topic to research</param>
    /// <param name="description">Brief description of what to focus on</param>
    /// <param name="audience">Target audience for the blog post</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A structured outline for the blog post</returns>
    public async Task<string> ResearchAndOutlineAsync(
        string topic, 
        string description, 
        string audience = "General",
        CancellationToken cancellationToken = default)
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

        _logger.LogInformation("Starting research for topic: {Topic}", topic);
        return await ExecutePromptAsync(prompt, cancellationToken);
    }
}