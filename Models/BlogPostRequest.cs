namespace BlogPostGenerator.Models;

/// <summary>
/// Represents a request to generate a blog post with specific parameters
/// </summary>
public class BlogPostRequest
{
    /// <summary>
    /// The main topic or subject of the blog post
    /// </summary>
    public string Topic { get; set; } = string.Empty;
    
    /// <summary>
    /// A brief description of what to focus on in the blog post
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The target audience for the blog post (e.g., "General", "Developers", "Business Professionals")
    /// </summary>
    public string TargetAudience { get; set; } = "General";
    
    /// <summary>
    /// The target word count for the blog post
    /// </summary>
    public int WordCount { get; set; } = 800;
    
    /// <summary>
    /// The tone of the blog post (e.g., "Professional", "Casual", "Technical")
    /// </summary>
    public string Tone { get; set; } = "Professional";
}