using System;
using System.Collections.Generic;
using Librariann.Models.DTOs.Common;
using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus;
#nullable enable

public sealed record LibrariannPlusAuditSeriesInfoDto : IUpdateExternalMetadataIds
{
    public int SeriesId { get; init; }
    public int LibraryId { get; init; }
    public string SeriesName { get; init; } = string.Empty;
    public bool IsMatched { get; init; }
    public int? AniListId { get; set; }
    public long? MalId { get; set; }
    public int? HardcoverId { get; set; }
    public long? MetronId { get; set; }
    public string? ComicVineId { get; set; }
    public int? MangaBakaId { get; set; }
    public int? CbrId { get; set; }
    public bool IsStandAlone { get; set; }
    [EnumDataType(typeof(MetadataProvider))]
    public MetadataProvider? MetadataProvider { get; set; }
    public DateTime? NextRefreshUtc { get; init; }
    public DateTime? LastRefreshedUtc { get; init; }
    public IList<LibrariannPlusAuditEntryDto> RecentEvents { get; init; } = [];
}