using Dnet.QdrantAdmin.Api.Infrastructure.Models;
using Dnet.QdrantAdmin.Api.Infrastructure.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Embeddings;

public class OpenAIEmbeddingProvider : IEmbeddingProvider
{
    private readonly EmbeddingProviderConfig _config;
    private readonly IServiceProvider _serviceProvider;

    public OpenAIEmbeddingProvider(EmbeddingProviderConfig config, IServiceProvider serviceProvider)
    {
        _config = config;
        _serviceProvider = serviceProvider;
    }

    public string Name => _config.Name;

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(IEnumerable<string> inputs, ModelConfig model, int dimension, CancellationToken cancellationToken = default)
    {
        var serviceId = EmbeddingService.GeneratorServiceId(Name, model.Model, dimension);

        var generator = _serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>(serviceId)
            ?? throw new InvalidOperationException($"No embedding generator is registered for provider '{Name}', model '{model.Model}' with {dimension} dimensions");

        var result = await generator.GenerateAsync(inputs, cancellationToken: cancellationToken);

        return result.Select(embedding => embedding.Vector).ToList();
    }
}
