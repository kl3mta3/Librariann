using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.API.Services.Metadata;

/// <summary>
/// Optional provider capability for expanding an author or series into its broader catalog.
/// Metadata providers that only support title lookup do not need to implement this interface.
/// </summary>
public interface IMetadataCatalogProvider
{
    Task<IReadOnlyCollection<MetadataCatalogItem>> GetCatalogAsync(MetadataCatalogRequest request,
        CancellationToken cancellationToken = default);
}

