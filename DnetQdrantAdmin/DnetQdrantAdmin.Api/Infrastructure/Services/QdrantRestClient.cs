using Dnet.QdrantAdmin.Api.Infrastructure.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Dnet.QdrantAdmin.Api.Infrastructure.Services;

/// <summary>
/// Thin client for the Qdrant REST API (port 6333), used for operations that the
/// Qdrant.Client gRPC package does not expose, such as listing payload indexes,
/// downloading snapshots and restoring snapshots from an uploaded file.
/// </summary>
public class QdrantRestClient
{
    private readonly HttpClient _httpClient;

    public QdrantRestClient(IOptions<QdrantConfig> config)
    {
        var baseUrl = config.Value.QdrantRestUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            var host = config.Value.QdrantServerHost;

            if (host.Contains("://"))
            {
                var uri = new Uri(host);
                baseUrl = $"{uri.Scheme}://{uri.Host}:6333";
            }
            else
            {
                baseUrl = $"http://{host}:6333";
            }
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public record CollectionIndexInfo(string FieldName, string FieldType);

    public record CollectionDetail(string Name, string Status, ulong PointsCount, ulong? VectorsSize);

    public record ClusterStatus(bool Enabled, int PeerCount);

    public async Task<IReadOnlyList<CollectionIndexInfo>> GetCollectionIndexesAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"collections/{Uri.EscapeDataString(collectionName)}/indexes", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Older Qdrant versions do not expose this endpoint: treat it as "not supported".
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return [];
            }

            response.EnsureSuccessStatusCode();
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        var indexes = new List<CollectionIndexInfo>();

        if (doc.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in result.EnumerateArray())
            {
                var fieldName = item.TryGetProperty("field_name", out var fn) ? fn.GetString() ?? string.Empty : string.Empty;
                var fieldType = item.TryGetProperty("field_type", out var ft) ? ft.GetString() ?? string.Empty : string.Empty;

                if (!string.IsNullOrEmpty(fieldName))
                {
                    indexes.Add(new CollectionIndexInfo(fieldName, fieldType));
                }
            }
        }

        return indexes;
    }

    public async Task<IReadOnlyList<CollectionDetail>> GetCollectionsDetailsAsync(IEnumerable<string> collectionNames, CancellationToken cancellationToken = default)
    {
        var details = new List<CollectionDetail>();

        foreach (var name in collectionNames)
        {
            using var response = await _httpClient.GetAsync($"collections/{Uri.EscapeDataString(name)}", cancellationToken);

            if (!response.IsSuccessStatusCode) continue;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            if (!doc.RootElement.TryGetProperty("result", out var result)) continue;

            var status = result.TryGetProperty("status", out var st) ? st.GetString() ?? "unknown" : "unknown";
            var pointsCount = result.TryGetProperty("points_count", out var pc) && pc.TryGetUInt64(out var pcValue) ? pcValue : 0;
            ulong? vectorsSize = result.TryGetProperty("vectors_size", out var vs) && vs.TryGetUInt64(out var vsValue) ? vsValue : null;

            details.Add(new CollectionDetail(name, status, pointsCount, vectorsSize));
        }

        return details;
    }

    public async Task<ClusterStatus> GetClusterStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("cluster", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ClusterStatus(false, 1);
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            if (!doc.RootElement.TryGetProperty("result", out var result)) return new ClusterStatus(false, 1);

            var status = result.TryGetProperty("status", out var st) ? st.GetString() ?? string.Empty : string.Empty;

            if (!string.Equals(status, "enabled", StringComparison.OrdinalIgnoreCase))
            {
                return new ClusterStatus(false, 1);
            }

            var peerCount = result.TryGetProperty("peers", out var peers) && peers.ValueKind == JsonValueKind.Object
                ? peers.EnumerateObject().Count()
                : 1;

            return new ClusterStatus(true, peerCount);
        }
        catch (HttpRequestException)
        {
            // Cluster endpoint unavailable: assume a standalone node.
            return new ClusterStatus(false, 1);
        }
    }

    public async Task<Stream> DownloadSnapshotAsync(string collectionName, string snapshotName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"collections/{Uri.EscapeDataString(collectionName)}/snapshots/{Uri.EscapeDataString(snapshotName)}",
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task UploadSnapshotAsync(string collectionName, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();

        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "snapshot", fileName);

        using var response = await _httpClient.PostAsync(
            $"collections/{Uri.EscapeDataString(collectionName)}/snapshots/upload?wait=true", form, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
