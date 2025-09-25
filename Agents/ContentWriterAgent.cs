using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace BlogPostGenerator.Agents;

/// <summary>
/// Content Writer Agent responsible for creating the main blog content from research outlines
/// </summary>
public class ContentWriterAgent
{
    private readonly Kernel _kernel;

    public ContentWriterAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Write blog post content based on a research outline
    /// </summary>
    /// <param name="outline">Research outline and structure</param>
    /// <param name="tone">Writing tone (Professional, Casual, Technical, etc.)</param>
    /// <param name="wordCount">Target word count</param>
    /// <returns>Complete blog post content in markdown format</returns>
    [KernelFunction, Description("Write blog post content based on research outline")]
    public async Task<string> WriteContentAsync(
        [Description("Research outline and structure")] string outline,
        [Description("Writing tone (Professional, Casual, Technical, etc.)")] string tone = "Professional",
        [Description("Target word count")] int wordCount = 800)
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
        
        Focus on creating valuable, engaging content that provides real insights to readers.
        """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
    }
}