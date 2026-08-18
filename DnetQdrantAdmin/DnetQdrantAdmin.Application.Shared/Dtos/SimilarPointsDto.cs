using Dnet.QdrantAdmin.Application.Shared.Enums;

namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class SimilarPointsDto
{
    public string CollectionName { get; set; } = string.Empty;

    public string QpointId { get; set; } = string.Empty;

    public PointIdType PointIdType { get; set; }

    public ulong Limit { get; set; } = 10;

    /// <summary>Optional Qdrant filter in JSON format.</summary>
    public string? FilterString { get; set; }

    public float? ScoreThreshold { get; set; }
}
