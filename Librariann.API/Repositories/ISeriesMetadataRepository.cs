using Librariann.Models.Entities.Metadata;

namespace Librariann.API.Repositories;

public interface ISeriesMetadataRepository
{
    void Update(SeriesMetadata seriesMetadata);
}
