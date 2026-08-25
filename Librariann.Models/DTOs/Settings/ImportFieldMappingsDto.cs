using Librariann.Models.DTOs.LibrariannPlus.Metadata;

namespace Librariann.Models.DTOs.Settings;

public sealed record ImportFieldMappingsDto
{
    /// <summary>
    /// Import settings
    /// </summary>
    public ImportSettingsDto Settings { get; init; }
    /// <summary>
    /// Data to import
    /// </summary>
    public FieldMappingsDto Data { get; init; }
}
