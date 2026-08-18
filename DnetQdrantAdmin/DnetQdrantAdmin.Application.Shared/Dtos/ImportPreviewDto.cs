namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class ImportPreviewDto
{
    public List<QpointDto> Points { get; set; } = new();

    public int SkippedCount { get; set; }

    public List<string> Errors { get; set; } = new();

    /// <summary>Columns detected in the source file (headers for CSV/TSV, property names for JSONL).</summary>
    public List<string> Headers { get; set; } = new();
}
