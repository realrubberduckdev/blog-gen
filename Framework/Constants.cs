namespace BlogPostGenerator.Framework;

/// <summary>
/// Contains constant values used throughout the blog post generator application
/// </summary>
public static class Constants
{
    /// <summary>
    /// Agent names and descriptions
    /// </summary>
    public static class Agents
    {
        public static class ContentWriter
        {
            public const string Name = "Content Writer Agent";
            public const string Description = "Writes the main blog content from outlines";
        }

        public static class Editor
        {
            public const string Name = "Editor Agent";
            public const string Description = "Reviews and polishes content for quality";
        }

        public static class MarkdownLinter
        {
            public const string Name = "Markdown Linter Agent";
            public const string Description = "Ensures proper markdown formatting standards";
        }

        public static class Research
        {
            public const string Name = "Research Agent";
            public const string Description = "Creates topic outlines and gathers information";
        }

        public static class SEO
        {
            public const string Name = "SEO Agent";
            public const string Description = "Optimizes content and generates metadata";
        }
    }
}
