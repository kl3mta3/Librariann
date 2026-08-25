using Librariann.Models.DTOs.Common;

namespace Librariann.Models.DTOs;

public sealed record UpdateSeriesMetadataDto
{
    public SeriesMetadataDto SeriesMetadata { get; set; } = null!;
}
