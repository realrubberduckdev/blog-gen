using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace BlogPostGenerator.Framework;

/// <summary>
/// Base class for all agents in the Microsoft Agent Framework implementation
/// </summary>
public abstract class BaseAgent
{
    protected readonly IChatClient _chatClient;
    protected readonly ILogger _logger;

    protected BaseAgent(IChatClient chatClient, ILogger logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    protected async Task<string> ExecutePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, prompt)
            };

            var response = await _chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);
            return response.Message.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing prompt for {AgentType}", GetType().Name);
            throw;
        }
    }
}