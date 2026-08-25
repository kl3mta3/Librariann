namespace Librariann.Models.DTOs;

public sealed record SeriesByIdsDto
{
    public int[] SeriesIds { get; init; } = default!;
}
