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

public class HardcoverScrobbleProviderService(ILogger<HardcoverScrobbleProviderService> logger, IUnitOfWork unitOfWork, ILibrariannPlusAuditService auditService)
    : ChapterScrobbleService<HardcoverScrobbleProviderService>(logger, unitOfWork, auditService)
{
    protected override ScrobbleProvider Provider => ScrobbleProvider.Hardcover;

    protected override IReadOnlyList<ScrobbleEventType> SupportedEvents =>
    [
        ScrobbleEventType.ChapterRead, ScrobbleEventType.AddWantToRead, ScrobbleEventType.RemoveWantToRead,
        ScrobbleEventType.ScoreUpdated, ScrobbleEventType.Review, ScrobbleEventType.ReadStatusUpdate
    ];

    protected override void SetScrobbleIds(ScrobbleEvent evt, Series series, Chapter chapter)
    {
        evt.HardcoverId = chapter.HardcoverId;
    }

    protected override bool HasRequiredIds(Chapter chapter)
    {
        return chapter.HardcoverId > 0;
    }

    // Hardcover's rate limit is enforced per-user (~60 requests/min), so each user is tracked independently
    public override RateProfile RateProfile => new(
        BaseInterval: TimeSpan.FromSeconds(1),
        Buffer: TimeSpan.FromMilliseconds(500),
        LowRateThreshold: 5,
        RebuildWait: TimeSpan.FromSeconds(60),
        Scope: RateScope.User);

    public override bool IsTokenValid(string token)
    {
        // PAT is always valid. When we switch to OAuth, they're also always valid (I.e. can't check)
        return true;
    }
}
