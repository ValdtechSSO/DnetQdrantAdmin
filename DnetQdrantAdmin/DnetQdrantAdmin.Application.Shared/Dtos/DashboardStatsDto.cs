namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class DashboardStatsDto
{
    public int CollectionCount { get; set; }

    public ulong PointsCount { get; set; }

    /// <summary>Total vector storage used by the collections, in bytes (null when the server does not report it).</summary>
    public ulong? StorageBytes { get; set; }

    /// <summary>Number of nodes in the Qdrant cluster (1 for a standalone deployment).</summary>
    public int PeerCount { get; set; } = 1;

    /// <summary>True when Qdrant runs in cluster (distributed) mode.</summary>
    public bool ClusterEnabled { get; set; }

    public int StatusGreen { get; set; }

    public int StatusYellow { get; set; }

    public int StatusRed { get; set; }

    public int ShardCount { get; set; }
}
