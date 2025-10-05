using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BlogPostGenerator.Services.Gemini;

/// <summary>
/// Google Gemini API client implementation of IChatClient
/// </summary>
public class GeminiChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly JsonSerializerOptions _jsonOptions;

    public GeminiChatClient(HttpClient httpClient, string apiKey, string model = "gemini-2.5-flash")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _model = model;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public ChatClientMetadata Metadata => new("Gemini", new Uri("https://generativelanguage.googleapis.com"), _model);

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        // doc reference https://ai.google.dev/gemini-api/docs/quickstart#make-first-request
        // and there is a free tier for limited usage https://ai.google.dev/gemini-api/docs/pricing
        var request = CreateGeminiRequest(chatMessages, options);
        var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
        
        using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent")
        {
            Content = httpContent
        };
        
        httpRequest.Headers.Add("x-goog-api-key", _apiKey);

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, _jsonOptions);

        return ConvertToChatCompletion(geminiResponse, options);
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For simplicity, we'll implement streaming by calling the non-streaming method
        // and yielding the result as a single update. A full implementation would use
        // the streaming endpoint.
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

    private GeminiRequest CreateGeminiRequest(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var contents = new List<GeminiContent>();
        
        foreach (var message in messages)
        {
            var parts = new List<GeminiPart>();
            
            foreach (var content in message.Contents)
            {
                if (content is TextContent textContent)
                {
                    parts.Add(new GeminiPart { Text = textContent.Text });
                }
                // Add support for other content types as needed
            }
            
            if (parts.Count > 0)
            {
                contents.Add(new GeminiContent { Parts = parts });
            }
        }

        var request = new GeminiRequest
        {
            Contents = contents
        };

        // Apply generation configuration if provided in options
        if (options != null)
        {
            var generationConfig = new GeminiGenerationConfig();
            
            if (options.Temperature.HasValue)
                generationConfig.Temperature = (float)options.Temperature.Value;
            
            if (options.MaxOutputTokens.HasValue)
                generationConfig.MaxOutputTokens = options.MaxOutputTokens.Value;
            
            if (options.TopP.HasValue)
                generationConfig.TopP = (float)options.TopP.Value;
            
            if (options.TopK.HasValue)
                generationConfig.TopK = options.TopK.Value;

            request.GenerationConfig = generationConfig;
        }

        return request;
    }

    private ChatCompletion ConvertToChatCompletion(GeminiResponse? geminiResponse, ChatOptions? options)
    {
        if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text is not string text)
        {
            throw new InvalidOperationException("Invalid response from Gemini API");
        }

        var message = new ChatMessage(ChatRole.Assistant, text);
        
        var usage = new UsageDetails
        {
            InputTokenCount = geminiResponse.UsageMetadata?.PromptTokenCount,
            OutputTokenCount = geminiResponse.UsageMetadata?.CandidatesTokenCount,
            TotalTokenCount = geminiResponse.UsageMetadata?.TotalTokenCount
        };

        return new ChatCompletion(message)
        {
            CompletionId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            ModelId = _model,
            FinishReason = ChatFinishReason.Stop,
            Usage = usage,
            RawRepresentation = geminiResponse
        };
    }
}

#region Gemini API Data Models

public class GeminiRequest
{
    public List<GeminiContent> Contents { get; set; } = new();
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

public class GeminiContent
{
    public List<GeminiPart> Parts { get; set; } = new();
}

public class GeminiPart
{
    public string? Text { get; set; }
}

public class GeminiGenerationConfig
{
    public float? Temperature { get; set; }
    public int? MaxOutputTokens { get; set; }
    public float? TopP { get; set; }
    public int? TopK { get; set; }
}

public class GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
    public GeminiUsageMetadata? UsageMetadata { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
    public string? FinishReason { get; set; }
}

public class GeminiUsageMetadata
{
    public int? PromptTokenCount { get; set; }
    public int? CandidatesTokenCount { get; set; }
    public int? TotalTokenCount { get; set; }
}

#endregion