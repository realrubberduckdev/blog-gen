# AI Agent Instructions for Blog Generator

## Project Overview
This is a multi-agent AI system that generates high-quality blog posts using **Microsoft Agent Framework**. The application uses a **4-stage pipeline** with specialized agents:

1. **ResearchAgent** - Creates topic outlines and gathers information
2. **ContentWriterAgent** - Writes the main blog content from outlines
3. **EditorAgent** - Reviews and polishes content for quality
4. **SEOAgent** - Optimizes content and generates metadata

## Architecture Patterns

### Agent Framework Architecture
- All agents inherit from `BaseAgent` and use `IChatClient` for AI interactions
- Each agent implements `IAgent` interface with standardized properties
- The orchestrator coordinates the pipeline: Research → Write → Edit → SEO
- Built on Microsoft.Extensions.AI abstractions

### Key Classes Structure
```
BlogPostRequest (input) → BlogPostOrchestrator → BlogPostResult (output)
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
# For Azure OpenAI (recommended for production)
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-azure-openai-key"

# Alternative: Legacy format (still supported for backward compatibility)
dotnet user-secrets set "OpenAI:ApiKey" "your-azure-openai-key"
```

### Configuration Options
The application now supports flexible configuration through `appsettings.json`:

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

**Local Model Configuration (Docker):**
```json
{
  "LocalModel": {
    "Endpoint": "http://localhost:11434",
    "ModelName": "llama3.1:8b",
    "ApiKey": "",
    "UseLocal": true
  }
}
```

Set `UseLocal: true` in `appsettings.Development.json` to use local models during development.

### Dependencies
- Target Framework: **.NET 9.0**
- Key packages: Microsoft.Extensions.AI, Microsoft.Extensions.Hosting
- Uses dependency injection pattern with `IServiceCollection`

## Code Conventions

### Agent Implementation Pattern
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

## Integration Points

### Microsoft Agent Framework Configuration
- Uses `IChatClient` abstraction for all AI interactions
- Support for Azure OpenAI, OpenAI, and local models through Microsoft.Extensions.AI
- API key retrieved from user secrets configuration
- Configuration supports multiple AI providers with unified interface

### Service Registration
All agents registered as transient services in DI container. The `IChatClient` instance is configured during application startup.

## Working with This Codebase

### Adding New Agents
1. Create class inheriting from `BaseAgent` and implementing `IAgent`
2. Register in DI container as transient
3. Add to orchestrator pipeline if needed

### Modifying Pipeline
The orchestrator defines the execution order. Each step passes its output to the next stage. Maintain this pattern for consistency.

### Error Handling
- Wrap agent calls in try-catch with structured logging
- Provide meaningful error messages for API configuration issues
- Graceful degradation with sensible defaults