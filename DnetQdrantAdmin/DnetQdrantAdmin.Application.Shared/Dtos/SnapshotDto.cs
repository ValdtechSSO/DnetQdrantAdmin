namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class SnapshotDto
{
    public string Name { get; set; } = string.Empty;

    public long Size { get; set; }

    public string CreationTime { get; set; } = string.Empty;
}

public class DeleteSnapshotDto
{
    public string CollectionName { get; set; } = string.Empty;

    public string SnapshotName { get; set; } = string.Empty;
}
