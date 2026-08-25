#nullable enable
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus.Metadata;



public sealed record SeriesRelationship
{
    public int AniListId { get; set; }
    public int? MalId { get; set; }
    /// <summary>
    /// MangaBaka series id. Often the only navigable id for MangaBaka-sourced relationships.
    /// </summary>
    public int? MangabakaId { get; set; }
    public ALMediaTitle SeriesName { get; set; }
    [EnumDataType(typeof(RelationKind))]
    public RelationKind Relation { get; set; }
    [EnumDataType(typeof(ScrobbleProvider))]
    public ScrobbleProvider Provider { get; set; }
    [EnumDataType(typeof(PlusMediaFormat))]
    public PlusMediaFormat Format { get; set; } = PlusMediaFormat.Manga;
    public ExternalSeriesDetailDto Series { get; set; }
    [EnumDataType(typeof(MetadataProvider))]
    public MetadataProvider MetadataProvider { get; set; }
}