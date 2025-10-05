# Google Gemini API Configuration

This blog generator now supports Google Gemini AI models in addition to Azure OpenAI and OpenAI.

## Setup Instructions

### 1. Get a Google AI API Key

1. Go to [Google AI Studio](https://aistudio.google.com/app/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy the generated API key

### 2. Configure the API Key

You can configure the Google Gemini API key in several ways:

#### Option A: User Secrets (Recommended for development)

```bash
dotnet user-secrets set "GoogleGemini:ApiKey" "your-google-ai-api-key-here"
```

#### Option B: Environment Variable

```bash
# Windows PowerShell
$env:GoogleGemini__ApiKey = "your-google-ai-api-key-here"

# Linux/Mac
export GoogleGemini__ApiKey="your-google-ai-api-key-here"
```

#### Option C: appsettings.json (Not recommended for production)

```json
{
  "GoogleGemini": {
    "ModelId": "gemini-2.5-flash",
    "ApiKey": "your-google-ai-api-key-here"
  }
}
```

### 3. Available Models

You can configure different Gemini models:

- `gemini-2.5-flash` (default) - Fast and efficient
- `gemini-2.5-pro` - More capable but slower
- `gemini-1.5-flash` - Previous generation fast model
- `gemini-1.5-pro` - Previous generation pro model

### 4. Configuration Priority

The application checks for AI providers in this order:

1. **Google Gemini** - If `GoogleGemini:ApiKey` is configured
2. **Azure OpenAI** - If `AzureOpenAI:Endpoint` and `AzureOpenAI:ApiKey` are configured
3. **OpenAI** - If `OpenAI:ApiKey` is configured

### 5. Running with Google Gemini

Once configured, simply run the application as usual:

```bash
# Using a JSON request file
dotnet run gemini-example-request.json

# Using command line arguments
dotnet run --topic "AI and Machine Learning" --description "Explore the future of AI"

# Interactive mode
dotnet run
```

### 6. Example Configuration File

See `gemini-example-request.json` for a sample blog post request that works well with Gemini models.

## API Usage and Costs

- Google Gemini API offers competitive pricing
- gemini-2.5-flash is optimized for speed and cost-efficiency
- gemini-2.5-pro provides higher quality outputs for complex tasks
- Check [Google AI Pricing](https://ai.google.dev/pricing) for current rates

## Troubleshooting

### Common Issues

1. **Invalid API Key**: Ensure your API key is correctly set and has not expired
2. **Model Not Found**: Verify the ModelId in your configuration matches available models
3. **Rate Limits**: Google AI has rate limits; consider implementing retry logic for production use

### Debug Information

The application will print which AI provider it's using at startup:

```text
Configuring Google Gemini with model: gemini-2.5-flash
```

If you see this message, Google Gemini is successfully configured and will be used for blog generation.