using System.ComponentModel.DataAnnotations;
using Librariann.Models.Entities.Enums;

namespace Librariann.Models.DTOs.Account;

public sealed record UpdateAgeRestrictionDto
{
    [EnumDataType(typeof(AgeRating))]
    [Required]
    public AgeRating AgeRating { get; set; }
    [Required]
    public bool IncludeUnknowns { get; set; }
}
