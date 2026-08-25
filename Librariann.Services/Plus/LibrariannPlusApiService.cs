#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Librariann.API.Database;
using Librariann.API.Services.Plus;
using Librariann.Common;
using Librariann.Common.Extensions;
using Librariann.Models.DTOs.Collection;
using Librariann.Models.DTOs.LibrariannPlus;
using Librariann.Models.DTOs.LibrariannPlus.ExternalMetadata;
using Librariann.Models.DTOs.LibrariannPlus.ExternalMetadata.Covers;
using Librariann.Models.DTOs.LibrariannPlus.License;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.DTOs.LibrariannPlus.OAuth;
using Librariann.Models.DTOs.LibrariannPlus.Scrobble;
using Librariann.Models.DTOs.Metadata.Matching;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.Entities.Enums;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Plus;

public class LibrariannPlusApiService(ILogger<LibrariannPlusApiService> logger, IUnitOfWork unitOfWork, IDataProtectionProvider dataProtectionProvider): ILibrariannPlusApiService
{
    private const string ScrobblingPath = "/api/scrobbling/";
    public const string ApiKeyDataProtectorName = "LibrariannPlus.ApiKey";

    private readonly IDataProtector _dataProtector = dataProtectionProvider.CreateProtector(ApiKeyDataProtectorName);

    public async Task<int> GetRateLimitAsync(string license, string token, CancellationToken ct = default)
    {
        var res = await Get(ScrobblingPath + "rate-limit?accessToken=" + token, license, token);
        var str = await res.GetStringAsync();
        return int.Parse(str);
    }


    public async Task<IList<MalStackDto>> GetMalStacksAsync(string malUsername, string license, CancellationToken ct = default)
    {
        return await $"{Configuration.LibrariannPlusApiUrl}/api/metadata/v2/stacks?username={malUsername}"
            .WithLibrariannPlusHeaders(license)
            .GetJsonAsync<IList<MalStackDto>>(cancellationToken: ct);
    }

    public async Task<IList<ExternalSeriesMatchDto>> MatchSeriesAsync(MatchSeriesRequestDto request,
        CancellationToken ct = default)
    {
        var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
        var token = (await unitOfWork.UserRepository.GetDefaultAdminUser(ct: ct))
            .ScrobbleProviders[ScrobbleProvider.AniList]
            .AuthenticationToken;

        return await (Configuration.LibrariannPlusApiUrl + "/api/metadata/v2/match-series")
            .WithLibrariannPlusHeaders(license, token)
            .PostJsonAsync(request, cancellationToken: ct)
            .ReceiveJson<IList<ExternalSeriesMatchDto>>();
    }

    public async Task<KPlusResult<SeriesDetailPlusApiDto?>> GetSeriesDetailV3Async(SeriesDetailRequestV3Dto request, CancellationToken ct = default)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;

            return await (Configuration.LibrariannPlusApiUrl + "/api/v3/Metadata/series-detail")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(request, cancellationToken: ct)
                .ReceiveJson<KPlusResult<SeriesDetailPlusApiDto?>>();
        }
        catch (FlurlHttpException ex)
        {
            // Surface the response body (e.g. "Unknown Series", "Too many Requests") rather than the generic
            // "Call failed with status code..." so callers can react to specific error markers.
            var body = (await ex.GetResponseStringAsync() ?? string.Empty).Trim('"');
            logger.LogError(ex, "There was an issue getting series detail from Librariann+ for Series ({SeriesName})", request.SeriesName);
            return KPlusResult<SeriesDetailPlusApiDto?>.Failure(string.IsNullOrEmpty(body) ? ex.Message : body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue getting series detail from Librariann+ for Series ({SeriesName})", request.SeriesName);
            return KPlusResult<SeriesDetailPlusApiDto?>.Failure(ex.Message);
        }
    }

    public async Task<KPlusResult<List<ExternalSeriesMatchDto>>> MatchSeriesV3Async(MatchRequestV3Dto request, CancellationToken ct = default)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;

            return await (Configuration.LibrariannPlusApiUrl + "/api/v3/Metadata/match")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(request, cancellationToken: ct)
                .ReceiveJson<KPlusResult<List<ExternalSeriesMatchDto>>>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue matching series from Librariann+ for Series ({SeriesName})", request.SeriesName);
            return KPlusResult<List<ExternalSeriesMatchDto>>.Failure(ex.Message);
        }
    }

    public async Task<ScrobbleResponseDto> PostScrobbleV3UpdateAsync(ScrobbleV3Dto data, string license, CancellationToken ct = default)
    {
        try
        {
            return await (Configuration.LibrariannPlusApiUrl + "/api/v3/Scrobble")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(data, cancellationToken: ct)
                .ReceiveJson<ScrobbleResponseDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue posting scrobble to Librariann+ for provider {Provider}", data.Provider);
            return new ScrobbleResponseDto
            {
                ErrorMessage = ex.Message,
                Successful = false
            };
        }
    }

    public async Task<KPlusResult<bool>> HasTokenExpiredForProviderAsync(ScrobbleProvider provider, string token, string license, CancellationToken ct = default)
    {
        try
        {
            return await (Configuration.LibrariannPlusApiUrl + "/api/v3/Scrobble/valid-access-token")
                .WithLibrariannPlusHeaders(license)
                .SetQueryParam("provider", provider)
                .SetQueryParam("accessToken", token)
                .GetJsonAsync<KPlusResult<bool>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue getting token validity from Librariann+ for provider {Provider}", provider);
            return KPlusResult<bool>.Failure(ex.Message);
        }
    }

    public async Task<KPlusResult<int>> GetRateLimitForProviderAsync(ScrobbleProvider provider, string token, string license, CancellationToken ct = default)
    {
        try
        {
            return await (Configuration.LibrariannPlusApiUrl + "/api/v3/Scrobble/rate-limit")
                .WithLibrariannPlusHeaders(license)
                .SetQueryParam("provider", provider)
                .SetQueryParam("accessToken", token)
                .GetJsonAsync<KPlusResult<int>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue getting rate limit from Librariann+ for provider {Provider}", provider);
            return KPlusResult<int>.Failure(ex.Message);
        }
    }

    public async Task<KPlusResult<IList<ExternalCoverResponseDto>>> GetCoverImagesAsync(ExternalCoverRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            var token = (await unitOfWork.UserRepository.GetDefaultAdminUser(ct: ct))
                .ScrobbleProviders[ScrobbleProvider.AniList]
                .AuthenticationToken;

            return await (Configuration.LibrariannPlusApiUrl + "/api/v3/metadata/covers")
                .WithLibrariannPlusHeaders(license, token)
                .PostJsonAsync(request, cancellationToken: ct)
                .ReceiveJson<KPlusResult<IList<ExternalCoverResponseDto>>>();
        }
        catch (Exception ex)
        {
            // TODO: How should I handle this? swallow and return nothing
            logger.LogError(ex, "There was an issue getting cover images from Librariann+ for Series ({SeriesName})", request.SeriesName);
            return KPlusResult<IList<ExternalCoverResponseDto>>.Failure(ex.Message);
        }
    }

    public async Task<KPlusResult<List<ExternalSeriesDetailDto>>> GetWantToRead(ScrobbleProvider provider, string token,
        string license, CancellationToken ct = default)
    {
        try
        {
            return await (Configuration.LibrariannPlusApiUrl + "/api/v3/Scrobble/want-to-read")
                .WithLibrariannPlusHeaders(license)
                .WithTimeout(TimeSpan.FromSeconds(120))
                .SetQueryParam("provider", provider)
                .SetQueryParam("accessToken", token)
                .GetJsonAsync<KPlusResult<List<ExternalSeriesDetailDto>>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue getting want to read from Librariann+ for provider {Provider}", provider);
            return KPlusResult<List<ExternalSeriesDetailDto>>.Failure(ex.Message);
        }
    }

    public async Task<KPlusResult<LibrariannPlusUserInfo>> GetUserInfo(ScrobbleProvider provider, string token, string license, CancellationToken ct = default)
    {
        try
        {
            return await (Configuration.LibrariannPlusApiUrl + "/api/v3/Scrobble/user-info")
                .WithLibrariannPlusHeaders(license)
                .SetQueryParam("provider", provider)
                .SetQueryParam("accessToken", token)
                .GetJsonAsync<KPlusResult<LibrariannPlusUserInfo>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue getting user info from Librariann+ for provider {Provider}", provider);
            return KPlusResult<LibrariannPlusUserInfo>.Failure(ex.Message);
        }
    }

    public async Task<LicenseInfoDto?> GetLicenseInfo(CancellationToken ct = default)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/license/info")
                .WithLibrariannPlusHeaders(license)
                .GetJsonAsync<LicenseInfoDto>(cancellationToken: ct);

            return response;
        } catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return null;
    }

    public async Task<LicenseInfoDto?> LinkDiscord(LinkDiscordRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/license/link-discord")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(request, cancellationToken: ct)
                .ReceiveJson<LicenseInfoDto>();

            return response;
        } catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return null;
    }

    /// <summary>
    /// Gets a snapshot of the Metadata providers operational health (average response time, last incident, overall status)
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IList<LibrariannPlusProviderHealthSnapshotDto>> GetProviderHealthSnapshot(CancellationToken ct = default)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/providerhealth/snapshot")
                .WithLibrariannPlusHeaders(license)
                .GetJsonAsync<IList<LibrariannPlusProviderHealthSnapshotDto>>(cancellationToken: ct);

            return response;
        } catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return [];
    }


    /// <summary>
    /// Gets a snapshot of the amount of usage this server has with Librariann+
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>Returns an empty object on errors</returns>
    public async Task<LibrariannPlusLicenseUsageDto> GetLicenseUsage(CancellationToken ct = default)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/stats/")
                .WithLibrariannPlusHeaders(license)
                .GetJsonAsync<KPlusResult<LibrariannPlusLicenseUsageDto>>(cancellationToken: ct);

            if (response.IsSuccess) return response.Data!;
            logger.LogError(response.ErrorMessage, "Unable to pull license usage data from Librariann+ API");
        } catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return new LibrariannPlusLicenseUsageDto()
        {
            GeneratedAtUtc =  DateTime.UtcNow,
            Stats = []
        };
    }

    public async Task<bool> CancelLicenseAsync(CancelLibrariannPlusLicenseDto dto, CancellationToken ct)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/license/cancel")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(dto, cancellationToken: ct)
                .ReceiveJson<KPlusResult<object>>();

            if (response.IsSuccess) return true;
            logger.LogError("Unable to cancel subscription on Librariann+ API: {Error}", response.ErrorMessage);
        } catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return false;
    }

    public async Task<IList<LibrariannPlusProductInfoDto>> GetProducts(CancellationToken ct = default)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            return await (Configuration.LibrariannPlusApiUrl + "/api/license/products")
                .WithLibrariannPlusHeaders(license)
                .GetJsonAsync<IList<LibrariannPlusProductInfoDto>>(cancellationToken: ct);
        } catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return [];
    }

    public async Task<string?> RenewLicenseAsync(RenewLibrariannPlusLicenseDto dto, CancellationToken ct)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/license/renew")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(dto, cancellationToken: ct)
                .ReceiveJson<KPlusResult<RenewSubscriptionResponseDto>>();

            if (response.IsSuccess) return response.Data?.CheckoutUrl;
            logger.LogError("Unable to renew subscription on Librariann+ API: {Error}", response.ErrorMessage);
        } catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return null;
    }

    public async Task<bool> ChangeEmail(ChangeEmailOnLicenseDto dto, CancellationToken ct)
    {
        try
        {
            var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/license/change-email")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(dto, cancellationToken: ct)
                .ReceiveJson<KPlusResult<bool>>(); // It just returns blank result

            if (response.IsSuccess) return response.IsSuccess;
            logger.LogError("Unable to change Librariann+ email: {Error}", response.ErrorMessage);
        } catch (FlurlHttpException e)
        {
            logger.LogError(e, "An error happened during the request to Librariann+ API");
        }

        return false;
    }

    public async Task<KPlusResult<string>> StartOAuthFlow(OAuthUpstream upstream, string instanceUrl, string apiKey,
        CancellationToken ct = default)
    {
        var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;

        var body = new StartOAuthFlowRequestDto
        {
            Upstream = upstream,
            InstanceUrl = instanceUrl,
            ApiKey = _dataProtector.Protect(apiKey)
        };

        try
        {
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/v3/oauth/start-flow")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(body, cancellationToken: ct)
                .ReceiveJson<KPlusResult<string>>();

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue starting the OAuth flow");
            return KPlusResult<string>.Failure(ex.Message);
        }
    }

    public async Task<KPlusResult<DateTime>> GetTokenExpiry(OAuthUpstream upstream, string accessToken, CancellationToken ct = default)
    {
        var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;

        try
        {
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/v3/oauth/token-expiration")
                .WithLibrariannPlusHeaders(license)
                .SetQueryParam("upstream", upstream)
                .SetQueryParam("accessToken", accessToken)
                .GetJsonAsync<KPlusResult<DateTime>>(cancellationToken: ct);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue starting refreshing tokens");
            return KPlusResult<DateTime>.Failure(ex.Message);
        }
    }

    public async Task<KPlusResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto requestDto, CancellationToken ct = default)
    {
        var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;

        try
        {
            var response = await (Configuration.LibrariannPlusApiUrl + "/api/v3/oauth/refresh-tokens")
                .WithLibrariannPlusHeaders(license)
                .PostJsonAsync(requestDto, cancellationToken: ct)
                .ReceiveJson<KPlusResult<TokenResponseDto>>();

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue starting refreshing tokens");
            return KPlusResult<TokenResponseDto>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Send a GET request to K+
    /// </summary>
    /// <param name="url">only path of the uri, the host is added</param>
    /// <param name="license"></param>
    /// <param name="aniListToken"></param>
    /// <returns></returns>
    private static async Task<IFlurlResponse> Get(string url, string license, string? aniListToken = null)
    {
        return await (Configuration.LibrariannPlusApiUrl + url)
            .WithLibrariannPlusHeaders(license, aniListToken)
            .GetAsync();
    }
}
