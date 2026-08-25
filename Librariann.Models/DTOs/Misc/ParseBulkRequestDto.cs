using System.Collections.Generic;
using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Misc;

public sealed record ParseBulkRequestDto
{
    public ICollection<string> Names { get; set; }
    [EnumDataType(typeof(LibraryType))]
    public LibraryType LibraryType { get; set; }
}