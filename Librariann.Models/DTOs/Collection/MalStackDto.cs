namespace Librariann.Models.DTOs.Collection;
#nullable enable

/// <summary>
/// Represents an Interest Stack from MAL
/// </summary>
public class MalStackDto
{
    public required string Title { get; set; }
    public required long StackId { get; set; }
    public required string Url { get; set; }
    public required string? Author { get; set; }
    public required int SeriesCount { get; set; }
    public required int RestackCount { get; set; }
    /// <summary>
    /// If an existing collection exists within Librariann
    /// </summary>
    /// <remarks>This is filled out from Librariann and not Librariann+</remarks>
    public int ExistingId { get; set; }
}
