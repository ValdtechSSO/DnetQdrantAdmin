using Dnet.QdrantAdmin.Api.Infrastructure.Embeddings;
using Dnet.QdrantAdmin.Api.Infrastructure.Models;
using Microsoft.Extensions.Options;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Services;

public interface IEmbeddingService
{
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(List<string> inputs, string? providerName, string llmModel, int dimension, CancellationToken cancellationToken = default);
}

public class EmbeddingService : IEmbeddingService
{
    private readonly IOptions<LlmProviderConfig> _llmProviderConfig;
    private readonly IEnumerable<IEmbeddingProvider> _providers;

    public EmbeddingService(IOptions<LlmProviderConfig> llmProviderConfig, IEnumerable<IEmbeddingProvider> providers)
    {
        _llmProviderConfig = llmProviderConfig;
        _providers = providers;
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(List<string> inputs, string? providerName, string llmModel, int dimension, CancellationToken cancellationToken = default)
    {
        var providerConfig = ResolveProviderConfig(providerName, llmModel)
            ?? throw new ArgumentException(providerName is null
                ? $"No embedding provider is configured for model '{llmModel}'"
                : $"Embedding provider '{providerName}' is not configured", nameof(providerName));

        var model = providerConfig.Models.FirstOrDefault(m => m.Model == llmModel)
            ?? throw new ArgumentException($"The LLM model '{llmModel}' is not configured in provider '{providerConfig.Name}'", nameof(llmModel));

        if (model.Dimensions.Any() && !model.Dimensions.Contains(dimension))
        {
            throw new ArgumentException($"The LLM model '{llmModel}' does not support {dimension}-dimension embeddings", nameof(dimension));
        }

        var provider = _providers.FirstOrDefault(p => string.Equals(p.Name, providerConfig.Name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No embedding provider implementation is registered for provider '{providerConfig.Name}' of type '{providerConfig.Type}'");

        return await provider.GenerateAsync(inputs, model, dimension, cancellationToken);
    }

    private EmbeddingProviderConfig? ResolveProviderConfig(string? providerName, string llmModel)
    {
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            return _llmProviderConfig.Value.Providers.FirstOrDefault(p => string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));
        }

        // No explicit provider: use the first provider that has the model configured.
        return _llmProviderConfig.Value.Providers.FirstOrDefault(p => p.Models.Any(m => m.Model == llmModel));
    }

    internal static string GeneratorServiceId(string providerName, string model, int dimension)
    {
        return $"{providerName}:{model}:{dimension}";
    }
}
