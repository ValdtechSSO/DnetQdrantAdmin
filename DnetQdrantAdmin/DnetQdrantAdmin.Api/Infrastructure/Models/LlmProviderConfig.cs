namespace Dnet.QdrantAdmin.Api.Infrastructure.Models;

public class LlmProviderConfig
{
    /// <summary>Embedding providers configured for the application.</summary>
    public List<EmbeddingProviderConfig> Providers { get; set; } = new();
}

public class EmbeddingProviderConfig
{
    /// <summary>Unique provider name, e.g. "openai", "azure", "ollama".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Provider type: OpenAI, AzureOpenAI, Ollama or FastEmbed.</summary>
    public string Type { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    /// <summary>Azure OpenAI endpoint or Ollama base URL.</summary>
    public string? Endpoint { get; set; }

    public List<ModelConfig> Models { get; set; } = new();
}
