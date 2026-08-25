using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services.Plus;
using Librariann.Common;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.LibrariannPlus.Scrobble;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using Librariann.Models.Entities.Scrobble;
using Librariann.Models.Entities.User;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Plus.ScrobbleService;

public class AniListScrobbleProviderService(ILogger<AniListScrobbleProviderService> logger, IUnitOfWork unitOfWork, ILibrariannPlusAuditService auditService)
    : SeriesScrobbleService<AniListScrobbleProviderService>(logger, unitOfWork, auditService)
{
    protected override ScrobbleProvider Provider => ScrobbleProvider.AniList;
    protected override IReadOnlyList<ScrobbleEventType> SupportedEvents =>
    [
        ScrobbleEventType.ChapterRead, ScrobbleEventType.AddWantToRead, ScrobbleEventType.RemoveWantToRead,
        ScrobbleEventType.ScoreUpdated, ScrobbleEventType.Review, ScrobbleEventType.ReadStatusUpdate
    ];
    protected override void SetScrobbleIds(ScrobbleEvent evt, Series series)
    {
        evt.AniListId = series.AniListId;
    }

    protected override bool HasRequiredIds(Series series)
    {
        return series.AniListId > 0;
    }

    // AniList's rate limit is enforced server-wide (~30 requests/min), shared across all users.
    // 30/min == one request every 2s
    public override RateProfile RateProfile => new(
        BaseInterval: TimeSpan.FromSeconds(2),
        Buffer: TimeSpan.FromMilliseconds(300),
        LowRateThreshold: 10,
        RebuildWait: TimeSpan.FromSeconds(60),
        Scope: RateScope.Server);

    public override bool IsTokenValid(string token)
    {
        return JwtHelper.IsTokenValid(token);
    }
}
