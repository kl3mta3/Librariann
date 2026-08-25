using Librariann.Models.Entities.Acquisition;

namespace Librariann.API.Services.Metadata;

public interface IMetadataProviderFactory
{
    IMetadataProvider Create(IntegrationProviderConfiguration configuration);
}
