using System.ComponentModel.DataAnnotations;

namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class ModelDto
{
    [Required]
    public string Model { get; set; } = string.Empty;

    /// <summary>Name of the embedding provider this model belongs to.</summary>
    public string ProviderName { get; set; } = string.Empty;

    [Required]
    public int Dimension { get; set; } = 1536;

    public List<int> Dimensions { get; set; } = new();

    public bool Default { get; set; }
}