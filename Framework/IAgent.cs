namespace BlogPostGenerator.Framework;

/// <summary>
/// Interface for all agents in the system
/// </summary>
public interface IAgent
{
    string Name { get; }
    string Description { get; }
}