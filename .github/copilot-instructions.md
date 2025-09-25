# AI Agent Instructions for Blog Generator

## Project Overview
This is a multi-agent AI system that generates high-quality blog posts using Microsoft Semantic Kernel. The application uses a **4-stage pipeline** with specialized agents:

1. **ResearchAgent** - Creates topic outlines and gathers information
2. **ContentWriterAgent** - Writes the main blog content from outlines
3. **EditorAgent** - Reviews and polishes content for quality
4. **SEOAgent** - Optimizes content and generates metadata

## Architecture Patterns

### Agent-Based Architecture
- All agents inherit `Kernel` dependency and use `[KernelFunction]` attributes
- Each agent has a single responsibility and communicates through the `BlogPostOrchestrator`
- The orchestrator coordinates the pipeline: Research → Write → Edit → SEO

### Key Classes Structure
```
BlogPostRequest (input) → BlogPostOrchestrator → BlogPostResult (output)
```

### Configuration & Secrets
- Uses **Azure OpenAI** (not regular OpenAI) with specific endpoint: `https://dp-openai1.openai.azure.com/`
- API keys stored in user secrets: `dotnet user-secrets set "OpenAI:ApiKey" "your-key"`
- Deployment name is hardcoded as `"gpt-4o-mini"`

## Development Workflows

### Building and Running
```bash
dotnet build
dotnet run
```

### Setting Up API Keys
```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-azure-openai-key"
```

### Dependencies
- Target Framework: **.NET 9.0**
- Key packages: Microsoft.SemanticKernel (v1.65.0), Azure OpenAI connectors
- Uses dependency injection pattern with `ServiceCollection`

## Code Conventions

### Agent Implementation Pattern
```csharp
public class SomeAgent
{
    private readonly Kernel _kernel;
    
    [KernelFunction, Description("What this function does")]
    public async Task<string> SomeActionAsync(
        [Description("Parameter description")] string param)
    {
        var prompt = $"""
        Multi-line prompt with {param}
        """;
        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
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

### Azure OpenAI Configuration
- Must use `AddAzureOpenAIChatCompletion()` method (not regular OpenAI)
- Endpoint and deployment name are environment-specific
- API key retrieved from user secrets configuration

### Service Registration
All agents registered as transient services in DI container. The kernel instance is registered as singleton and shared across all agents.

## Working with This Codebase

### Adding New Agents
1. Create class with `Kernel` dependency
2. Add `[KernelFunction]` methods with descriptions
3. Register in `ServiceCollection` as transient
4. Add to orchestrator pipeline if needed

### Modifying Pipeline
The orchestrator defines the execution order. Each step passes its output to the next stage. Maintain this pattern for consistency.

### Error Handling
- Wrap orchestrator calls in try-catch with structured logging
- Provide meaningful error messages for API key configuration issues
- Graceful degradation in JSON parsing with sensible defaults