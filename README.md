# 🤖 AI Blog Generator

> Multi-agent AI system that creates high-quality blog posts using **Microsoft Agent Framework**

## ✨ Features

🔍 **Research Agent** - Creates topic outlines & gathers information  
✍️ **Content Writer** - Writes engaging blog content  
📝 **Editor Agent** - Reviews & polishes for quality  
🔧 **Markdown Linter** - Ensures proper formatting  
🚀 **SEO Agent** - Optimizes content & generates metadata  

## 🛠️ Tech Stack

- **.NET 9.0** with **Microsoft Agent Framework**
- **Microsoft.Extensions.AI** for AI integration
- **Azure OpenAI** / **OpenAI** / **Local Model** support
- **Agent-based architecture** with dependency injection

## 🚀 Quick Start

```powershell
# Clone & setup
git clone <repo-url>
cd blog-gen

# Configure API key
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"

# Run with JSON file (recommended)
dotnet run sample-request.json

# Or run interactively
dotnet run
```

## 📝 Usage Options

### 1. JSON File Input (Recommended)

Create a JSON request file for complex blog posts:

```json
{
  "Topic": "Avoid Storing Secrets in PowerShell's Command History",
  "Description": "When using PowerShell scripts to manage secrets like API tokens, passwords, or other sensitive data...",
  "TargetAudience": "Software Engineers and DevOps Professionals",
  "WordCount": 1200,
  "Tone": "Professional and Technical"
}
```

```powershell
# Use specific JSON file
dotnet run my-blog-request.json

# Auto-discovery (searches for blog-request.json, request.json, sample-request.json)
dotnet run
```

### 2. Command Line Arguments

For simple requests:

```powershell
dotnet run --topic "API Security Best Practices" --description "Comprehensive guide to securing APIs" --audience "Developers" --wordcount 1500 --tone "Technical"
```

### 3. Configuration File

Add defaults to `appsettings.json`:

```json
{
  "BlogPostDefaults": {
    "TargetAudience": "Software Engineers",
    "WordCount": 1000,
    "Tone": "Professional"
  }
}
```

### 4. Interactive Mode

Run without arguments to enter interactive mode.

## ⚙️ Configuration

The application now uses **Microsoft Agent Framework** with Microsoft.Extensions.AI:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-endpoint.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini"
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

## 📊 Pipeline

**Topic** → **Research** → **Write** → **Edit** → **Lint** → **SEO** → **📝 Blog Post**

---

Built with 💙 using Microsoft Agent Framework and Microsoft.Extensions.AI
