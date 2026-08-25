using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.API.Services.Metadata;

public interface IMetadataLookupService
{
    Task<MetadataLookupResponse> SearchAsync(int userId, MetadataLookupRequest request,
        CancellationToken cancellationToken = default);
}
