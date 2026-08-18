using Dnet.QdrantAdmin.Api.Infrastructure.Models;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Embeddings;

/// <summary>
/// Abstraction over an embedding provider (OpenAI, Azure OpenAI, Ollama, Qdrant FastEmbed, ...).
/// Each implementation generates float vectors for a list of text inputs using one of its
/// configured models.
/// </summary>
public interface IEmbeddingProvider
{
    string Name { get; }

    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(IEnumerable<string> inputs, ModelConfig model, int dimension, CancellationToken cancellationToken = default);
}
