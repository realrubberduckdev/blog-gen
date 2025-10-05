using BlogPostGenerator.Framework;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BlogPostGenerator.Agents;

/// <summary>
/// Content Writer Agent responsible for creating the main blog content from research outlines
/// </summary>
public class ContentWriterAgent : BaseAgent, IAgent
{
    public string Name => Constants.Agents.ContentWriter.Name;
    public string Description => Constants.Agents.ContentWriter.Description;

    public ContentWriterAgent(IChatClient chatClient, ILogger<ContentWriterAgent> logger)
        : base(chatClient, logger)
    {
    }

    /// <summary>
    /// Write blog post content based on a research outline
    /// </summary>
    /// <param name="outline">Research outline and structure</param>
    /// <param name="tone">Writing tone (Professional, Casual, Technical, etc.)</param>
    /// <param name="wordCount">Target word count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete blog post content in markdown format</returns>
    public async Task<string> WriteContentAsync(
        string outline,
        string tone = "Professional",
        int wordCount = 800,
        CancellationToken cancellationToken = default)
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
        - Do NOT wrap the output in markdown code blocks (```markdown or ```)

        Focus on creating valuable, engaging content that provides real insights to readers.
        """;

        _logger.LogInformation("Writing content with tone: {Tone}, target words: {WordCount}", tone, wordCount);
        return await ExecutePromptAsync(prompt, cancellationToken);
    }
}