namespace Dnet.QdrantAdmin.Api.Infrastructure.Models;

public class QdrantConfig
{
    public string QdrantServerHost { get; set; } = string.Empty;

    /// <summary>
    /// Optional base URL of the Qdrant REST API (defaults to http://{QdrantServerHost}:6333).
    /// Used for operations not exposed by the gRPC client, such as listing payload
    /// indexes, downloading snapshots and restoring snapshots from an uploaded file.
    /// </summary>
    public string? QdrantRestUrl { get; set; }
}
