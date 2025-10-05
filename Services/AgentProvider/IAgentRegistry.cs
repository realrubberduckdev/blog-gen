using BlogPostGenerator.Framework;

namespace BlogPostGenerator.Services.AgentProvider;

/// <summary>
/// Provides type-safe access to registered agents without string lookups or casting
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Gets an agent of the specified type from the service container
    /// </summary>
    /// <typeparam name="T">The type of agent to retrieve</typeparam>
    /// <returns>The agent instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when the agent type is not registered</exception>
    T GetAgent<T>() where T : class, IAgent;
}