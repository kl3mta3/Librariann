using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Person;

public sealed record ApplyAuthorMetadataDto
{
    [Range(1, int.MaxValue)]
    public int PersonId { get; init; }

    [Required]
    public string ProviderKey { get; init; } = string.Empty;

    [Required]
    public string ExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Replace populated editable fields. False preserves curated metadata and fills only missing fields.
    /// </summary>
    public bool OverwriteExisting { get; init; }
}

