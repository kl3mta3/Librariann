using System;
using System.Collections.Generic;

namespace Librariann.Models.DTOs.Person;

/// <summary>
/// A normalized author match returned by an external metadata provider.
/// </summary>
public sealed record AuthorMetadataCandidateDto
{
    public string ProviderKey { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ExternalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Aliases { get; init; } = [];
    public string BirthDate { get; init; } = string.Empty;
    public string DeathDate { get; init; } = string.Empty;
    public string TopWork { get; init; } = string.Empty;
    public int WorkCount { get; init; }
    public Uri? PortraitUri { get; init; }
    public Uri? DetailsUri { get; init; }
    public int MatchScore { get; init; }
    public IReadOnlyCollection<string> MatchReasons { get; init; } = [];
}

