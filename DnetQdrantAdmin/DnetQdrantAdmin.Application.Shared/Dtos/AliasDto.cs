using System.ComponentModel.DataAnnotations;

namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class AliasDto
{
    public string Name { get; set; } = string.Empty;

    public string CollectionName { get; set; } = string.Empty;
}

public class CreateAliasDto
{
    [Required]
    public string AliasName { get; set; } = string.Empty;

    [Required]
    public string CollectionName { get; set; } = string.Empty;
}

public class DeleteAliasDto
{
    [Required]
    public string AliasName { get; set; } = string.Empty;
}
