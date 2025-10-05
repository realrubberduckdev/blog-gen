using BlogPostGenerator.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace BlogPostGenerator.Services.AgentProvider;

/// <summary>
/// Type-safe agent registry that uses the service provider to retrieve agents
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    private readonly IServiceProvider _serviceProvider;

    public AgentRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets an agent of the specified type from the service container
    /// </summary>
    /// <typeparam name="T">The type of agent to retrieve</typeparam>
    /// <returns>The agent instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when the agent type is not registered</exception>
    public T GetAgent<T>() where T : class, IAgent
    {
        var agent = _serviceProvider.GetRequiredService<T>();
        return agent;
    }
}