using Qdrant.Client.Grpc;

namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class UpdateCollectionDto
{
    public string Name { get; set; } = string.Empty;

    public HnswConfigDiff? HnswConfigDiff { get; set; }

    public OptimizersConfigDiff? OptimizersConfigDiff { get; set; }

    public CollectionParamsDiff? CollectionParamsDiff { get; set; }

    public TimeSpan? Timeout { get; set; }
}
