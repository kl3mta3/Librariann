using System;
using System.Collections.Generic;
using Librariann.API.Database;
using Librariann.API.Services.Plus;
using Librariann.Common.Helpers;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using Librariann.Models.Entities.Scrobble;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Plus.ScrobbleService;

public class MyAnimeListScrobbleProviderService(ILogger<MyAnimeListScrobbleProviderService> logger, IUnitOfWork unitOfWork, ILibrariannPlusAuditService auditService)
    : SeriesScrobbleService<MyAnimeListScrobbleProviderService>(logger, unitOfWork, auditService)
{
    protected override ScrobbleProvider Provider => ScrobbleProvider.Mal;
    protected override IReadOnlyList<ScrobbleEventType> SupportedEvents =>
    [
        ScrobbleEventType.AddWantToRead, ScrobbleEventType.ChapterRead, ScrobbleEventType.ReadStatusUpdate,
        ScrobbleEventType.RemoveWantToRead, ScrobbleEventType.ScoreUpdated
    ];

    protected override void SetScrobbleIds(ScrobbleEvent evt, Series series)
    {
        evt.MalId = series.MalId;
    }

    protected override bool HasRequiredIds(Series series)
    {
        return series.MalId > 0;
    }

    public override RateProfile RateProfile => new(
        BaseInterval: TimeSpan.FromSeconds(1),
        Buffer: TimeSpan.FromMilliseconds(500),
        LowRateThreshold: 5,
        RebuildWait: TimeSpan.FromSeconds(60),
        Scope: RateScope.Server);

    public override bool IsTokenValid(string token)
    {
        return JwtHelper.IsTokenValid(token);
    }
}
