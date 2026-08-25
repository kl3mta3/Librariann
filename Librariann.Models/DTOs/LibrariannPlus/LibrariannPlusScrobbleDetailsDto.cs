using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using Librariann.Models.Entities.Enums.UserPreferences;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus;
#nullable enable

/// <summary>
/// Scrobble-specific context surfaced on a Librariann+ audit entry. Projected from <see cref="AuditLogScrobbleParamsDto"/>.
/// </summary>
public sealed record LibrariannPlusScrobbleDetailsDto
{
    [EnumDataType(typeof(ScrobbleEventType))]
    public ScrobbleEventType? ScrobbleEventType { get; init; }
    public int? ChapterNumber { get; init; }
    public float? VolumeNumber { get; init; }
    public float? PercentRead { get; init; }
    public float? Rating { get; init; }
    public string? ReviewBody { get; init; }
    [EnumDataType(typeof(ScrobbleReadStatus))]
    public ScrobbleReadStatus? ReadStatus { get; init; }
    [EnumDataType(typeof(ScrobbleProvider))]
    public ScrobbleProvider Provider { get; init; } = ScrobbleProvider.AniList;
    [EnumDataType(typeof(LibraryType))]
    public LibraryType LibraryType { get; init; } = LibraryType.Manga;
}