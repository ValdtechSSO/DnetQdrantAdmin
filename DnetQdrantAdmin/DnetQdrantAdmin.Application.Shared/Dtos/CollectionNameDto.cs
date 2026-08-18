using System.ComponentModel.DataAnnotations;

namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class CollectionNameDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
