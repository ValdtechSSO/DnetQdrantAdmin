namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class ImportConfigDto
{
    /// <summary>Csv, Tsv or Jsonl.</summary>
    public string FileFormat { get; set; } = "Csv";

    /// <summary>Name of the column/property used as the embedding text. When empty, the first one is used.</summary>
    public string? TextField { get; set; }

    /// <summary>Columns/properties included in the payload. When empty, all the remaining ones are used.</summary>
    public List<string> PayloadFields { get; set; } = new();

    public bool HasHeader { get; set; } = true;
}
