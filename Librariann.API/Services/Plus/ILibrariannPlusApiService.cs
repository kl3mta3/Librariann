using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

namespace Librariann.API.Services.Plus;

/// <summary>
/// All Http requests to K+ should be contained in this service, the service will not handle any errors.
/// This is expected from the caller.
///
/// Methods returning <see cref="KPlusResult{T}"/> will NOT thrown.
/// </summary>
public interface ILibrariannPlusApiService
{
    [Obsolete]
    Task<int> GetRateLimitAsync(string license, string token, CancellationToken ct = default);
    Task<IList<MalStackDto>> GetMalStacksAsync(string malUsername, string license, CancellationToken ct = default);
    Task<IList<ExternalSeriesMatchDto>> MatchSeriesAsync(MatchSeriesRequestDto request, CancellationToken ct = default);
    Task<KPlusResult<SeriesDetailPlusApiDto?>> GetSeriesDetailV3Async(SeriesDetailRequestV3Dto request, CancellationToken ct = default);
    Task<KPlusResult<List<ExternalSeriesMatchDto>>> MatchSeriesV3Async(MatchRequestV3Dto request, CancellationToken ct = default);
    Task<ScrobbleResponseDto> PostScrobbleV3UpdateAsync(ScrobbleV3Dto data, string license, CancellationToken ct = default);
    Task<KPlusResult<bool>> HasTokenExpiredForProviderAsync(ScrobbleProvider provider, string token, string license, CancellationToken ct = default);
    Task<KPlusResult<int>> GetRateLimitForProviderAsync(ScrobbleProvider provider, string token, string license, CancellationToken ct = default);
    Task<KPlusResult<IList<ExternalCoverResponseDto>>> GetCoverImagesAsync(ExternalCoverRequestDto request, CancellationToken ct = default);
    Task<KPlusResult<List<ExternalSeriesDetailDto>>> GetWantToRead(ScrobbleProvider provider, string token, string license, CancellationToken ct = default);
    Task<KPlusResult<LibrariannPlusUserInfo>> GetUserInfo(ScrobbleProvider provider, string token, string license, CancellationToken ct = default);
    Task<LicenseInfoDto?> GetLicenseInfo(CancellationToken ct = default);
    Task<LicenseInfoDto?> LinkDiscord(LinkDiscordRequestDto request, CancellationToken ct = default);
    Task<IList<LibrariannPlusProviderHealthSnapshotDto>> GetProviderHealthSnapshot(CancellationToken ct = default);
    Task<LibrariannPlusLicenseUsageDto> GetLicenseUsage(CancellationToken ct = default);
    Task<bool> CancelLicenseAsync(CancelLibrariannPlusLicenseDto dto, CancellationToken ct);
    Task<IList<LibrariannPlusProductInfoDto>> GetProducts(CancellationToken ct = default);
    Task<string?> RenewLicenseAsync(RenewLibrariannPlusLicenseDto dto, CancellationToken ct);
    Task<bool> ChangeEmail(ChangeEmailOnLicenseDto dto, CancellationToken ct);

    /// <summary>
    /// Starts the OAuth flow for the given upstream. Returns a JWT token to be use as authentication for the redirect to K+
    /// Which handles the OAuth flow with the upstream, and redirect back to OAuth/callback
    /// </summary>
    /// <param name="upstream"></param>
    /// <param name="instanceUrl"></param>
    /// <param name="apiKey"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<KPlusResult<string>> StartOAuthFlow(OAuthUpstream upstream, string instanceUrl, string apiKey, CancellationToken ct = default);
    /// <summary>
    /// Returns the expiry date of the given access token. Either by reading from JWT or calling the introspect endpoint (OAuth)
    /// </summary>
    /// <param name="upstream"></param>
    /// <param name="accessToken"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<KPlusResult<DateTime>> GetTokenExpiry(OAuthUpstream upstream, string accessToken, CancellationToken ct = default);
    /// <summary>
    /// Runs the OAuth refresh token flow
    /// </summary>
    /// <param name="requestDto"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<KPlusResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto requestDto, CancellationToken ct = default);
}
