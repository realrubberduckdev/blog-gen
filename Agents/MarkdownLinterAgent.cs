using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace BlogPostGenerator.Agents;

/// <summary>
/// Markdown Linter Agent responsible for ensuring markdown content follows proper formatting standards and has no linting issues
/// </summary>
public class MarkdownLinterAgent
{
    private readonly Kernel _kernel;

    public MarkdownLinterAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Lint and fix markdown formatting issues in the blog post content
    /// </summary>
    /// <param name="content">Markdown content to lint and fix</param>
    /// <returns>Properly formatted markdown content with linting issues resolved</returns>
    [KernelFunction, Description("Lint and fix markdown formatting issues in blog post content")]
    public async Task<string> LintAndFixMarkdownAsync(
        [Description("Markdown content to lint and fix")] string content)
    {
        var prompt = $"""
        You are a markdown linter and formatter. Review the following markdown content and fix any linting issues:

        CONTENT TO LINT:
        {content}

        Check for and fix these markdown linting issues:
        1. **Headers**: Ensure proper header hierarchy (h1 → h2 → h3, no skipping levels)
        2. **Spacing**: Add proper blank lines around headers, code blocks, and lists
        3. **Lists**: Consistent indentation and formatting for ordered/unordered lists
        4. **Links**: Proper link formatting [text](url) and reference-style links
        5. **Code blocks**: Proper fencing with language specification where appropriate
        6. **Emphasis**: Consistent use of *italic* and **bold** formatting
        7. **Line length**: Break overly long lines at natural points (aim for ~80-100 characters)
        8. **Trailing spaces**: Remove unnecessary trailing whitespace
        9. **Empty lines**: Remove multiple consecutive empty lines
        10. **Special characters**: Proper escaping of markdown special characters in text

        Additional formatting improvements:
        - Ensure code snippets have proper syntax highlighting language tags
        - Make sure table formatting is consistent and aligned
        - Verify that blockquotes use proper > formatting
        - Check that horizontal rules use consistent syntax (---)
        - Return ONLY the corrected markdown content. Do not include explanations or notes about what was changed.
        - Do NOT wrap the output in markdown code blocks (```markdown or ```).
        - The output should be clean, properly formatted markdown that passes standard linting rules.
        """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString().Trim();
    }

    /// <summary>
    /// Validate markdown structure and provide recommendations
    /// </summary>
    /// <param name="content">Markdown content to validate</param>
    /// <returns>Validation report with issues found and recommendations</returns>
    [KernelFunction, Description("Validate markdown structure and provide linting recommendations")]
    public async Task<string> ValidateMarkdownStructureAsync(
        [Description("Markdown content to validate")] string content)
    {
        var prompt = $"""
        You are a markdown validator. Analyze the following markdown content and provide a brief validation report:

        CONTENT TO VALIDATE:
        {content}

        Check for these potential issues:
        1. Header hierarchy problems (skipped levels, inconsistent structure)
        2. Formatting inconsistencies (mixed emphasis styles, irregular spacing)
        3. Link issues (broken syntax, missing alt text for images)
        4. Code block problems (missing language tags, inconsistent fencing)
        5. List formatting issues (inconsistent bullets, poor indentation)
        6. Accessibility concerns (missing alt text, poor heading structure)

        Provide a concise report in this format:
        VALIDATION STATUS: [PASS/ISSUES_FOUND]
        
        ISSUES DETECTED:
        - [List any issues found, or "None" if content is clean]
        
        RECOMMENDATIONS:
        - [Brief suggestions for improvement, or "Content follows markdown best practices" if clean]
        """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString().Trim();
    }
}