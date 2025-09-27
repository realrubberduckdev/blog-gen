# 🤖 AI Blog Generator

> Multi-agent AI system that creates high-quality blog posts using Microsoft Semantic Kernel

## ✨ Features

🔍 **Research Agent** - Creates topic outlines & gathers information  
✍️ **Content Writer** - Writes engaging blog content  
📝 **Editor Agent** - Reviews & polishes for quality  
🔧 **Markdown Linter** - Ensures proper formatting  
🚀 **SEO Agent** - Optimizes content & generates metadata  

## 🛠️ Tech Stack

- **.NET 9.0** with Microsoft Semantic Kernel
- **Azure OpenAI** integration
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

Edit `appsettings.json` for Azure OpenAI or local model setup:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-endpoint.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini"
  }
}
```

## 📊 Pipeline

**Topic** → **Research** → **Write** → **Edit** → **Lint** → **SEO** → **📝 Blog Post**

---

*Built with 💙 using Microsoft Semantic Kernel*
