using System.Collections.Generic;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus.ExternalMetadata;
#nullable enable

public sealed record MatchRequestV3Dto: MetadataRequest
{
    [EnumDataType(typeof(MetadataProvider))]
    public required MetadataProvider Provider { get; set; }
    public required string SeriesName { get; set; }
    public List<string> AlternativeNames { get; set; } = [];
    public int? Year { get; set; }
    public string? Query { get; set; }
    [EnumDataType(typeof(PlusMediaFormat))]
    public PlusMediaFormat Format { get; set; }
}