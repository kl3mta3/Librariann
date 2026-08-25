using Librariann.API.Repositories;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Database.Repositories;



public class SeriesMetadataRepository(DataContext context) : ISeriesMetadataRepository
{
    public void Update(SeriesMetadata seriesMetadata)
    {
        context.SeriesMetadata.Update(seriesMetadata);
    }
}
