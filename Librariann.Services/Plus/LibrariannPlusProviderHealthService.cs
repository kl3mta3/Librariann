using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyCaching.Core;
using Flurl.Http;
using Librariann.API.Services.Plus;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.LibrariannPlus;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Plus;

public class LibrariannPlusProviderHealthService(
    IEasyCachingProviderFactory cachingProviderFactory,
    ILibrariannPlusApiService librariannPlusApiService,
    ILogger<LibrariannPlusProviderHealthService> logger)
    : ILibrariannPlusProviderHealthService
{
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(45);
    private const string CacheKey = "provider-health";

    public async Task<IList<LibrariannPlusProviderHealthSnapshotDto>> GetProviderHealthSnapshot(bool forceCheck = false, CancellationToken ct = default)
    {
        var provider = cachingProviderFactory.GetCachingProvider(EasyCacheProfiles.ProviderHealth);

        if (!forceCheck)
        {
            var cached = await provider.GetAsync<IList<LibrariannPlusProviderHealthSnapshotDto>>(CacheKey, ct);
            if (cached.HasValue) return cached.Value;
        }

        try
        {
            var response = await librariannPlusApiService.GetProviderHealthSnapshot(ct);
            await provider.FlushAsync(ct);
            await provider.SetAsync(CacheKey, response, _cacheTimeout, ct);
            return response;
        }
        catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return [];
    }
}
