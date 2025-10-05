using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BlogPostGenerator.Services;

/// <summary>
/// Local AI model client implementation of IChatClient for OpenAI-compatible APIs
/// </summary>
public class LocalChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalChatClient(HttpClient httpClient, string modelName, string baseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    public ChatClientMetadata Metadata => new("Local", new Uri(_baseUrl), _modelName);

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        // doc references https://docs.docker.com/ai/model-runner/api-reference/
        var request = CreateLocalRequest(chatMessages, options);
        var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
        
        using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/engines/llama.cpp/v1/chat/completions")
        {
            Content = httpContent
        };

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var localResponse = JsonSerializer.Deserialize<LocalResponse>(responseContent, _jsonOptions);

        return ConvertToChatCompletion(localResponse, options);
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For simplicity, we'll implement streaming by calling the non-streaming method
        // and yielding the result as a single update. A full implementation would use
        // the streaming endpoint with stream=true parameter.
        var response = await CompleteAsync(chatMessages, options, cancellationToken);
        
        yield return new StreamingChatCompletionUpdate
        {
            CompletionId = response.CompletionId,
            CreatedAt = response.CreatedAt,
            FinishReason = response.FinishReason,
            Contents = response.Message.Contents,
            Role = response.Message.Role,
            ModelId = response.ModelId,
            RawRepresentation = response.RawRepresentation
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(HttpClient))
            return _httpClient;
        
        return null;
    }

    public void Dispose()
    {
        // HttpClient is typically managed by DI container, so we don't dispose it here
    }

    private LocalRequest CreateLocalRequest(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var localMessages = new List<LocalMessage>();
        
        foreach (var message in messages)
        {
            var content = string.Empty;
            
            foreach (var messageContent in message.Contents)
            {
                if (messageContent is TextContent textContent)
                {
                    content += textContent.Text;
                }
                // Add support for other content types as needed
            }
            
            if (!string.IsNullOrEmpty(content))
            {
                localMessages.Add(new LocalMessage 
                { 
                    Role = ConvertRole(message.Role), 
                    Content = content 
                });
            }
        }

        var request = new LocalRequest
        {
            Model = _modelName,
            Messages = localMessages,
            Stream = false
        };

        // Apply generation configuration if provided in options
        if (options != null)
        {
            if (options.Temperature.HasValue)
                request.Temperature = (float)options.Temperature.Value;
            
            if (options.MaxOutputTokens.HasValue)
                request.MaxTokens = options.MaxOutputTokens.Value;
            
            if (options.TopP.HasValue)
                request.TopP = (float)options.TopP.Value;
        }

        return request;
    }

    private string ConvertRole(ChatRole role)
    {
        return role.Value switch
        {
            "user" => "user",
            "assistant" => "assistant",
            "system" => "system",
            _ => "user"
        };
    }

    private ChatCompletion ConvertToChatCompletion(LocalResponse? localResponse, ChatOptions? options)
    {
        if (localResponse?.Choices?.FirstOrDefault()?.Message?.Content is not string text)
        {
            throw new InvalidOperationException("Invalid response from local AI model");
        }

        var message = new ChatMessage(ChatRole.Assistant, text);
        
        var usage = new UsageDetails
        {
            InputTokenCount = localResponse.Usage?.PromptTokens,
            OutputTokenCount = localResponse.Usage?.CompletionTokens,
            TotalTokenCount = localResponse.Usage?.TotalTokens
        };

        return new ChatCompletion(message)
        {
            CompletionId = localResponse.Id ?? Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            ModelId = localResponse.Model ?? _modelName,
            FinishReason = ConvertFinishReason(localResponse.Choices?.FirstOrDefault()?.FinishReason),
            Usage = usage,
            RawRepresentation = localResponse
        };
    }

    private ChatFinishReason? ConvertFinishReason(string? finishReason)
    {
        return finishReason switch
        {
            "stop" => ChatFinishReason.Stop,
            "length" => ChatFinishReason.Length,
            "content_filter" => ChatFinishReason.ContentFilter,
            "tool_calls" => ChatFinishReason.ToolCalls,
            _ => ChatFinishReason.Stop
        };
    }
}

#region Local API Data Models

public class LocalRequest
{
    public string Model { get; set; } = string.Empty;
    public List<LocalMessage> Messages { get; set; } = new();
    public float? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public float? TopP { get; set; }
    public bool Stream { get; set; } = false;
}

public class LocalMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class LocalResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long? Created { get; set; }
    public string? Model { get; set; }
    public List<LocalChoice>? Choices { get; set; }
    public LocalUsage? Usage { get; set; }
}

public class LocalChoice
{
    public int? Index { get; set; }
    public LocalMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

public class LocalUsage
{
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
}

#endregion