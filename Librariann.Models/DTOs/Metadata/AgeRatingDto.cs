using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Metadata;

public sealed record AgeRatingDto
{
    [EnumDataType(typeof(AgeRating))]
    public AgeRating Value { get; set; }
    public required string Title { get; set; }
}