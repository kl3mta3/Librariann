using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.API.Services.Metadata;

public interface IMetadataApplicationService
{
    Task<ApplyMetadataResponse> ApplyAsync(int userId, ApplyMetadataRequest request,
        CancellationToken cancellationToken = default);
}
