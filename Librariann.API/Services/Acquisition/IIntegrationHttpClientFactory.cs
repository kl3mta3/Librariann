using System.Net.Http;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IIntegrationHttpClientFactory
{
    HttpClient Create(IntegrationProviderConfiguration configuration);
}

