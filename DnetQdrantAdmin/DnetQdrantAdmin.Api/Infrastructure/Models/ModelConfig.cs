namespace Dnet.QdrantAdmin.Api.Infrastructure.Models;

public class ModelConfig
{
    public string Model { get; set; } = string.Empty;

    public List<int> Dimensions { get; set; } = new();

    public bool Default { get; set; }
}