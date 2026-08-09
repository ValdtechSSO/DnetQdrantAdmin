namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class ImportPreviewDto
{
    public List<QpointDto> Points { get; set; } = new();

    public int SkippedCount { get; set; }

    public List<string> Errors { get; set; } = new();
}
