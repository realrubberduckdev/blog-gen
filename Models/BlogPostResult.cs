namespace BlogPostGenerator.Models;

/// <summary>
/// Represents the result of a blog post generation process
/// </summary>
public class BlogPostResult
{
    /// <summary>
    /// The SEO-optimized title of the blog post
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// The main content of the blog post in markdown format
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// List of relevant tags/keywords for the blog post
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// SEO meta description for the blog post
    /// </summary>
    public string MetaDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// A brief summary of the blog post content
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}