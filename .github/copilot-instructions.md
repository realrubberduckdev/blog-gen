# AI Agent Instructions for Blog Generator

## Project Overview
This is a multi-agent AI system that generates high-quality blog posts using **Microsoft Agent Framework**. The application uses a **5-stage pipeline** with specialized operations:

1. **Research** - Creates topic outlines and gathers information using IChatClient
2. **Content Writing** - Writes the main blog content from outlines  
3. **Content Editing** - Reviews and polishes content for quality
4. **Markdown Linting** - Formats and validates markdown structure
5. **SEO Optimization** - Optimizes content and generates metadata

## Architecture Patterns

### Agent Framework Architecture
- Built on Microsoft.Extensions.AI abstractions with direct `IChatClient` usage
- Agent classes available (`ResearchAgent`, `ContentWriterAgent`, etc.) but pipeline uses `IChatClient` directly
- The service coordinates the pipeline: Research → Write → Edit → Lint → SEO
- Each stage uses specialized system prompts with the same `IChatClient` instance

### Key Classes Structure
```
BlogPostRequest (input) → BlogGenerationService → BlogPostResult (output)
```

### Configuration & Secrets
- Uses **Microsoft.Extensions.AI** with support for Azure OpenAI, OpenAI, and local models
- API keys stored in user secrets: `dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"`
- Configuration supports multiple AI providers through unified IChatClient interface

### Input Methods
- **Primary**: JSON files for complex blog requests with verbose descriptions
- **Fallback**: Command line arguments, configuration defaults, or interactive input
- **Auto-discovery**: Searches for `blog-request.json`, `request.json`, `sample-request.json`
- **Validation**: JSON schema validation with meaningful error messages

## Development Workflows

### Building and Running
```bash
dotnet build
dotnet run                    # Interactive mode or auto-discovery
dotnet run sample-request.json    # JSON file input (recommended)
dotnet run --topic "My Topic" --description "Description"  # Command line
```

### Setting Up API Keys
```bash
# For Google Gemini (primary option)
dotnet user-secrets set "GoogleAI:ApiKey" "your-gemini-api-key"

# For Azure OpenAI (alternative)
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-azure-openai-key"

# For OpenAI (fallback)
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key"
```

### Configuration Options
The application supports multiple AI providers through `appsettings.json`:

**Google Gemini Configuration (Primary):**
```json
{
  "GoogleAI": {
    "ModelId": "gemini-2.5-flash",
    "ApiKey": ""
  }
}
```

**Azure OpenAI Configuration:**
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://dp-openai1.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini",
    "ApiKey": ""
  }
}
```

**Local Model Configuration:**
```json
{
  "LocalModel": {
    "Endpoint": "http://localhost:12434",
    "ModelName": "ai/gemma3",
    "ApiKey": "not-required",
    "UseLocal": false
  }
}
```

**Blog Post Defaults:**
```json
{
  "BlogPostDefaults": {
    "TargetAudience": "General",
    "WordCount": 500,
    "Tone": "Professional"
  }
}
```

Configuration priority: Local → Google Gemini → Azure OpenAI → OpenAI fallback.

### Dependencies
- Target Framework: **.NET 9.0**
- Core packages: Microsoft.Extensions.AI (9.0.1-preview), Microsoft.Extensions.Hosting (9.0.0)
- AI providers: Azure.AI.OpenAI (2.1.0), custom Gemini and local model clients
- Configuration: Microsoft.Extensions.Configuration.* packages
- JSON: System.Text.Json (9.0.9)
- Uses dependency injection pattern with `IServiceCollection`

## Code Conventions

### Service Implementation Pattern
```csharp
public class BlogGenerationService
{
    private readonly IChatClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlogGenerationService> _logger;
    
    public BlogGenerationService(IChatClient client, IConfiguration configuration, ILogger<BlogGenerationService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }
    
    // Pipeline stages use direct IChatClient calls with specialized system prompts
    private async Task<string> ExecuteStageAsync(string systemPrompt, string userPrompt)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };
        var response = await _client.CompleteAsync(messages);
        return response.Message.Text ?? "";
    }
}
```

### Agent Implementation Pattern (Available but not used in pipeline)
```csharp
public class SomeAgent : BaseAgent, IAgent
{
    public string Name => "Agent Name";
    public string Description => "Agent description";
    
    public SomeAgent(IChatClient chatClient, ILogger<SomeAgent> logger) 
        : base(chatClient, logger) { }
    
    public async Task<string> SomeActionAsync(string param, CancellationToken cancellationToken = default)
    {
        var prompt = $"Process {param}";
        return await ExecutePromptAsync(prompt, cancellationToken);
    }
}
```

### JSON Response Handling
- SEO agent returns JSON that requires careful parsing with fallbacks
- Uses `JsonSerializer.Deserialize<JsonElement>` with try-catch for resilient parsing
- Always provide fallback values when JSON parsing fails

### Logging Pattern
- Structured logging with `ILogger<T>` throughout the orchestrator
- Log each pipeline step: "Step 1: Research...", "Step 2: Writing...", etc.

### Error Handling
- Wrap service calls in try-catch with structured logging
- Provide meaningful error messages for API configuration issues
- Graceful degradation with sensible defaults for JSON parsing failures
- Comprehensive timing and performance logging throughout pipeline

## Integration Points

### Microsoft Agent Framework Configuration
- Uses `IChatClient` abstraction for all AI interactions
- Multi-provider support: Google Gemini, Azure OpenAI, OpenAI, and local models
- Provider selection based on configuration availability (priority order)
- API keys retrieved from user secrets or configuration
- Custom implementations for Google Gemini and local models

### Service Registration
- `IChatClient` registered as singleton with provider auto-detection
- `BlogGenerationService` registered as transient
- Provider-specific clients: `GeminiChatClient`, `LocalChatClient`
- Built-in Azure OpenAI and OpenAI support via Microsoft.Extensions.AI

## Working with This Codebase

### Current Architecture
The application currently uses `BlogGenerationService` with direct `IChatClient` calls rather than individual agent instances. Each pipeline stage uses specialized system prompts:

1. **Research Stage**: Creates outlines with research specialist prompt
2. **Writing Stage**: Generates content from outline with content writer prompt  
3. **Editing Stage**: Reviews and improves content with editor prompt
4. **Linting Stage**: Formats and validates markdown with linter prompt
5. **SEO Stage**: Optimizes content and generates metadata with SEO specialist prompt

### Adding New Pipeline Stages
1. Add new stage in `BlogGenerationService.RunAsync()` method
2. Create specialized system prompt for the stage
3. Use existing `IChatClient` instance with `CompleteAsync()` pattern
4. Add timing and logging following existing patterns

### Adding New Agents (Alternative Approach)
1. Create class inheriting from `BaseAgent` and implementing `IAgent`
2. Add agent methods using `ExecutePromptAsync()` from base class
3. Register in DI container as transient (see existing agent classes)
4. Integrate into service or use independently

### Modifying Pipeline
The `BlogGenerationService` defines the execution order. Each stage passes its output to the next stage. Maintain this pattern and preserve structured logging for consistency.