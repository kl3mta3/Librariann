using Librariann.Models.Entities.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IDownloadClientFactory
{
    IDownloadClient Create(IntegrationProviderConfiguration configuration);
}
