using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.API.Services.Metadata;

public interface IMetadataProvider : IDisposable
{
    string Key { get; }
    string Name { get; }
    IReadOnlySet<LibrariannMediaType> SupportedMediaTypes { get; }
    Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NormalizedMetadataCandidate>> SearchAsync(MetadataLookupRequest request,
        CancellationToken cancellationToken = default);
}
