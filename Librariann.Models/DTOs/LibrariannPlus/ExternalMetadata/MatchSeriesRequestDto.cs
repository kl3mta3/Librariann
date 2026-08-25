using System.Collections.Generic;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus.ExternalMetadata;
#nullable enable

/// <summary>
/// Represents a request to match some series from Librariann to an external id which K+ uses.
/// </summary>
public sealed record MatchSeriesRequestDto
{
    public required string SeriesName { get; set; }
    public ICollection<string> AlternativeNames { get; set; } = [];
    public int Year { get; set; } = 0;
    public string? Query { get; set; }
    public int? AniListId { get; set; }
    public long? MalId { get; set; }
    public string? HardcoverSlug { get; set; }
    public int? MangabakaId { get; set; }
    public int? CbrId { get; set; }
    public string? CbrSlug { get; set; }
    [EnumDataType(typeof(PlusMediaFormat))]
    public PlusMediaFormat Format { get; set; }
}