using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services.Plus;
using Librariann.Common;
using Librariann.Models.DTOs.LibrariannPlus.Scrobble;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using Librariann.Models.Entities.Scrobble;
using Librariann.Models.Entities.User;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Plus.ScrobbleService;

public class MangabakaScrobbleProviderService(ILogger<MangabakaScrobbleProviderService> logger, IUnitOfWork unitOfWork, ILibrariannPlusAuditService auditService)
    : SeriesScrobbleService<MangabakaScrobbleProviderService>(logger, unitOfWork, auditService)
{
    protected override ScrobbleProvider Provider => ScrobbleProvider.MangaBaka;
    protected override IReadOnlyList<ScrobbleEventType> SupportedEvents =>
    [
        ScrobbleEventType.ChapterRead, ScrobbleEventType.AddWantToRead, ScrobbleEventType.RemoveWantToRead,
        ScrobbleEventType.ScoreUpdated, ScrobbleEventType.ReadStatusUpdate, ScrobbleEventType.Review
    ];
    protected override void SetScrobbleIds(ScrobbleEvent evt, Series series)
    {
        evt.MangabakaId = series.MangaBakaId;
    }

    protected override bool HasRequiredIds(Series series)
    {
        return series.MangaBakaId > 0;
    }

    // MangaBaka is technically unlimited and server-wide (API keys), but we still pace it to be polite (~80/min)
    public override RateProfile RateProfile => new(
        BaseInterval: TimeSpan.FromMilliseconds(500),
        Buffer: TimeSpan.FromMilliseconds(250),
        LowRateThreshold: 5,
        RebuildWait: TimeSpan.FromSeconds(60),
        Scope: RateScope.Server);

    public override bool IsTokenValid(string token)
    {
        // We're using ApiKeys, always valid
        return true;
    }
}
