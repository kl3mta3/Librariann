using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Metadata;

public sealed record PublicationStatusDto
{
    [EnumDataType(typeof(PublicationStatus))]
    public PublicationStatus Value { get; set; }
    public required string Title { get; set; }
}