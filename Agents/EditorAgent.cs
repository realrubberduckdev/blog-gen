using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace BlogPostGenerator.Agents;

/// <summary>
/// Editor Agent responsible for reviewing and polishing blog content for quality and consistency
/// </summary>
public class EditorAgent
{
    private readonly Kernel _kernel;

    public EditorAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Review and edit blog post for quality and consistency
    /// </summary>
    /// <param name="content">Blog post content to review</param>
    /// <returns>Improved blog post content with editor notes</returns>
    [KernelFunction, Description("Review and edit blog post for quality and consistency")]
    public async Task<string> ReviewAndEditAsync(
        [Description("Blog post content to review")] string content)
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

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
    }
}