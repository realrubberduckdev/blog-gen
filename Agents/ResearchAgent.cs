using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace BlogPostGenerator.Agents;

/// <summary>
/// Research Agent responsible for gathering information and creating blog post outlines
/// </summary>
public class ResearchAgent
{
    private readonly Kernel _kernel;

    public ResearchAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Research a topic and create a comprehensive blog post outline
    /// </summary>
    /// <param name="topic">The topic to research</param>
    /// <param name="description">Brief description of what to focus on</param>
    /// <param name="audience">Target audience for the blog post</param>
    /// <returns>A structured outline for the blog post</returns>
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