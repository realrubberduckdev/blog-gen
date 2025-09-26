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

# Run
dotnet run
```

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
