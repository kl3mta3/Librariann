using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IDownloadClient : IDisposable
{
    string ProviderKey { get; }
    DownloadClientKind Kind { get; }
    DownloadProtocol Protocol { get; }
    Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default);
    Task<string> AddDownloadAsync(DownloadGrabRequest request, CancellationToken cancellationToken = default);
    Task<DownloadClientItem?> GetStatusAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DownloadClientItem>> GetCompletedAsync(CancellationToken cancellationToken = default);
    Task RemoveAsync(string externalId, bool deleteData, CancellationToken cancellationToken = default);
}
