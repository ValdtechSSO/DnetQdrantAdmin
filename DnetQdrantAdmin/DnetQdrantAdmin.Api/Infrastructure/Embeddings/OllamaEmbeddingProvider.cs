using Dnet.QdrantAdmin.Api.Infrastructure.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Embeddings;

public class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly EmbeddingProviderConfig _config;
    private readonly HttpClient _httpClient;

    public OllamaEmbeddingProvider(EmbeddingProviderConfig config)
    {
        _config = config;

        var endpoint = string.IsNullOrWhiteSpace(config.Endpoint) ? "http://localhost:11434" : config.Endpoint;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(endpoint.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public string Name => _config.Name;

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(IEnumerable<string> inputs, ModelConfig model, int dimension, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = model.Model,
            input = inputs.ToList()
        };

        using var response = await _httpClient.PostAsJsonAsync("api/embed", payload, cancellationToken);

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        if (!doc.RootElement.TryGetProperty("embeddings", out var embeddings) || embeddings.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"Ollama returned an unexpected response for model '{model.Model}'");
        }

        var result = new List<ReadOnlyMemory<float>>();

        foreach (var embedding in embeddings.EnumerateArray())
        {
            var vector = embedding.EnumerateArray().Select(v => v.GetSingle()).ToArray();

            result.Add(vector);
        }

        return result;
    }
}
