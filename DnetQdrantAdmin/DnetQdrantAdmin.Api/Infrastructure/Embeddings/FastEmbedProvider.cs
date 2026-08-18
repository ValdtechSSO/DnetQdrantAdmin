using Dnet.QdrantAdmin.Api.Infrastructure.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Embeddings;

/// <summary>
/// Embedding provider backed by the FastEmbed engine embedded in the Qdrant server.
/// Requires Qdrant to be started with FastEmbed enabled.
/// </summary>
public class FastEmbedProvider : IEmbeddingProvider
{
    private readonly EmbeddingProviderConfig _config;
    private readonly HttpClient _httpClient;

    public FastEmbedProvider(EmbeddingProviderConfig config)
    {
        _config = config;

        var endpoint = config.Endpoint ?? "http://localhost:6333";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(endpoint.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public string Name => _config.Name;

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(IEnumerable<string> inputs, ModelConfig model, int dimension, CancellationToken cancellationToken = default)
    {
        // The Qdrant FastEmbed endpoint processes one text per request.
        var tasks = inputs.Select(input => GenerateSingleAsync(input, model.Model, cancellationToken));

        var vectors = await Task.WhenAll(tasks);

        return vectors;
    }

    private async Task<ReadOnlyMemory<float>> GenerateSingleAsync(string input, string model, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model,
            text = input
        };

        using var response = await _httpClient.PostAsJsonAsync("embeddings", payload, cancellationToken);

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        if (!doc.RootElement.TryGetProperty("result", out var result))
        {
            throw new InvalidOperationException($"FastEmbed returned an unexpected response for model '{model}'");
        }

        // The result may be a flat array or an object with a "dense" array.
        var dense = result.ValueKind == JsonValueKind.Array
            ? result
            : result.TryGetProperty("dense", out var denseProperty)
                ? denseProperty
                : throw new InvalidOperationException($"FastEmbed returned an unexpected result shape for model '{model}'");

        return dense.EnumerateArray().Select(v => v.GetSingle()).ToArray();
    }
}
