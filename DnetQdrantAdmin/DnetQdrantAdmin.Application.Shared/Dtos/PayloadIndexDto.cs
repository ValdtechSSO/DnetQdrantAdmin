using System.ComponentModel.DataAnnotations;

namespace Dnet.QdrantAdmin.Application.Shared.Dtos;

public class PayloadIndexDto
{
    public string FieldName { get; set; } = string.Empty;

    public string FieldType { get; set; } = string.Empty;
}

public class CreatePayloadIndexDto
{
    [Required]
    public string CollectionName { get; set; } = string.Empty;

    [Required]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    public string FieldType { get; set; } = string.Empty;
}

public class DeletePayloadIndexDto
{
    [Required]
    public string CollectionName { get; set; } = string.Empty;

    [Required]
    public string FieldName { get; set; } = string.Empty;
}
