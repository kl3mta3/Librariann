using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.Common.Helpers;
using Librariann.Models.Constants;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Client;
using Librariann.Models.DTOs.Filtering.v2.Requests;
using Librariann.Models.Entities.Enums;
using Librariann.Server.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

/// <summary>
/// Versioned, presentation-neutral API surface for the Librariann embedded shell and external clients such as a
/// future Plex integration. Normal authentication, library grants, and content restrictions still apply.
/// </summary>
[Route("api/v1/client")]
public sealed class ClientV1Controller(IUnitOfWork unitOfWork) : BaseApiController
{
    [HttpGet("home")]
    public async Task<ActionResult<ClientHomeDto>> GetHome([FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 50);
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Librariann-Api-Version"] = "1";
        var serverId = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync(cancellationToken)).InstallId;
        var page = new UserParams {PageNumber = 1, PageSize = take};
        var libraryEntities = (await unitOfWork.LibraryRepository.GetLibrariesForUserIdAsync(UserId,
            cancellationToken)).ToArray();
        var libraryTypes = libraryEntities.ToDictionary(library => library.Id, library => library.Type);
        var libraries = libraryEntities
            .OrderBy(library => library.Name)
            .Select(library => new ClientLibraryDto($"{serverId}:library:{library.Id}", library.Id, library.Name, library.Type,
                $"/api/image/library-cover?libraryId={library.Id}", $"/library/{library.Id}"))
            .ToArray();
        var onDeck = await unitOfWork.SeriesRepository.GetOnDeckAsync(UserId, 0, page, cancellationToken);
        var recentlyAdded = await unitOfWork.SeriesRepository.GetRecentlyAddedAsync(UserId, page,
            new SeriesFilterV2Dto(), cancellationToken);
        var favoriteGenres = await unitOfWork.SeriesRepository.GetRecentlyAddedInFavoriteGenresAsync(UserId, page,
            cancellationToken);
        var recentlyDownloaded = await unitOfWork.SeriesRepository.GetRecentlyDownloadedAsync(UserId, page,
            cancellationToken);
        var missingItems = await unitOfWork.SeriesRepository.GetMissingFromMonitoredSeriesAsync(UserId, page,
            cancellationToken);
        var becauseYouRead = await unitOfWork.SeriesRepository.GetBecauseYouReadAsync(UserId, page,
            cancellationToken);
        var followedAuthors = await unitOfWork.SeriesRepository.GetNewFromFollowedAuthorsAsync(UserId, page,
            cancellationToken);
        var nextInSeries = await unitOfWork.SeriesRepository.GetNextInSeriesAsync(UserId, page,
            cancellationToken);

        var capabilities = new ClientCapabilitiesDto(
            true,
            HasCapability(PolicyConstants.DownloadRole),
            HasCapability(PolicyConstants.SearchIndexersRole),
            HasCapability(PolicyConstants.GrabReleasesRole),
            HasCapability(PolicyConstants.ManageMetadataRole),
            HasCapability(PolicyConstants.ManageLibrariesRole),
            HasCapability(PolicyConstants.ManageAcquisitionRole));
        var rails = new List<ClientRailDto>
        {
            new("continue-reading", "Continue Reading", "Items with reading progress for this user.",
                onDeck.Select(series => ToClientItem(serverId, series, libraryTypes.GetValueOrDefault(series.LibraryId),
                    "Continue where you left off.")).ToArray()),
            new("recently-added", "Recently Added", "New files in libraries this user can access.",
                recentlyAdded.Select(series => ToClientItem(serverId, series, libraryTypes.GetValueOrDefault(series.LibraryId),
                    "Recently added to your library.")).ToArray()),
        };
        if (favoriteGenres.Count > 0)
        {
            rails.Insert(1, new ClientRailDto("favorite-genres", "New in Genres You Love",
                "New additions matching genres this user has hearted.",
                favoriteGenres.Select(series => ToClientItem(serverId, series,
                    libraryTypes.GetValueOrDefault(series.LibraryId),
                    "Matches one of your favorite genres.")).ToArray()));
        }
        if (nextInSeries.Count > 0)
        {
            rails.Insert(1, new ClientRailDto("next-in-series", "Next In Series",
                "Unread sequels to series this user has finished.",
                nextInSeries.Select(series => ToClientItem(serverId, series,
                    libraryTypes.GetValueOrDefault(series.LibraryId),
                    "A sequel to a series you finished.")).ToArray()));
        }
        if (recentlyDownloaded.Count > 0)
        {
            rails.Insert(1, new ClientRailDto("recently-downloaded", "Recently Downloaded",
                "Successfully imported acquisitions available to this user.",
                recentlyDownloaded.Select(series => ToClientItem(serverId, series,
                    libraryTypes.GetValueOrDefault(series.LibraryId),
                    "Recently downloaded and imported into your library.")).ToArray()));
        }
        if (followedAuthors.Count > 0)
        {
            rails.Insert(1, new ClientRailDto("followed-authors", "New From Authors You Follow",
                "Recent library additions written by authors this user follows.",
                followedAuthors.Select(series => ToClientItem(serverId, series,
                    libraryTypes.GetValueOrDefault(series.LibraryId),
                    "Written by an author you follow.")).ToArray()));
        }
        if (becauseYouRead.Count > 0)
        {
            rails.Add(new ClientRailDto("because-you-read", "Because You Read...",
                "Local recommendations based on genres in this user's reading history.",
                becauseYouRead.Select(series => ToClientItem(serverId, series,
                    libraryTypes.GetValueOrDefault(series.LibraryId),
                    "Shares genres with books you have read.")).ToArray()));
        }
        var clientMissingItems = missingItems.Select(item => new ClientMissingItemDto(
            $"{serverId}:wanted:{item.WantedItemId}",
            item.WantedItemId,
            item.MonitoringTargetId,
            item.SourceSeriesId,
            item.LibraryId,
            item.SourceSeriesTitle,
            item.MissingTitle,
            item.Author,
            item.Series,
            item.Sequence,
            item.PublicationYear,
            $"/api/image/series-cover?seriesId={item.SourceSeriesId}",
            $"/library/{item.LibraryId}/series/{item.SourceSeriesId}",
            "Missing from a monitored series in your library.")).ToArray();
        return Ok(new ClientHomeDto("1", "Librariann", serverId, "/embed", capabilities, libraries, rails,
            clientMissingItems));
    }

    /// <summary>
    /// Returns a filesystem-neutral, permission-filtered manifest for offline clients. The service worker does not
    /// cache this response or media automatically; a user-authorized client must opt in and use the supplied API URLs.
    /// </summary>
    [HttpGet("series/{seriesId:int}/offline-manifest")]
    [SeriesAccess]
    [Authorize(PolicyGroups.DownloadPolicy)]
    public async Task<ActionResult<ClientOfflineManifestDto>> GetOfflineManifest(int seriesId,
        CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Librariann-Api-Version"] = "1";

        var series = await unitOfWork.SeriesRepository.GetSeriesDtoByIdAsync(seriesId, UserId, cancellationToken);
        if (series == null) return NotFound();

        var serverId = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync(cancellationToken)).InstallId;
        var volumes = await unitOfWork.VolumeRepository.GetVolumesDtoAsync(seriesId, UserId, ct: cancellationToken);
        var parts = volumes
            .SelectMany(volume => volume.Chapters)
            .OrderBy(chapter => chapter.SortOrder)
            .ThenBy(chapter => chapter.Id)
            .Select(chapter =>
            {
                var format = chapter.Format ?? MangaFormat.Unknown;
                var readerKind = format switch
                {
                    MangaFormat.Epub => "book",
                    MangaFormat.Pdf => "pdf",
                    _ => "manga",
                };
                return new ClientOfflinePartDto(
                    $"{serverId}:chapter:{chapter.Id}",
                    chapter.Id,
                    chapter.VolumeId,
                    string.IsNullOrWhiteSpace(chapter.TitleName) ? chapter.Title : chapter.TitleName,
                    chapter.SortOrder,
                    format,
                    chapter.Pages,
                    chapter.PagesRead,
                    chapter.Files.Sum(file => file.Bytes),
                    chapter.LastModifiedUtc,
                    $"/api/image/chapter-cover?chapterId={chapter.Id}",
                    $"/library/{series.LibraryId}/series/{seriesId}/{readerKind}/{chapter.Id}",
                    $"/api/download/chapter?chapterId={chapter.Id}");
            })
            .ToArray();
        var revisionUtc = parts.Length == 0 ? DateTime.UnixEpoch : parts.Max(part => part.LastModifiedUtc);

        return Ok(new ClientOfflineManifestDto(
            "1",
            serverId,
            $"{serverId}:series:{seriesId}",
            seriesId,
            series.Name ?? string.Empty,
            revisionUtc,
            parts.Sum(part => part.Bytes),
            "/api/reader/get-progress?chapterId={chapterId}",
            "/api/reader/progress",
            parts));
    }

    private bool HasCapability(string role) =>
        UserContext.HasAnyRole(PolicyConstants.AdminRole, role);

    private static ClientMediaItemDto ToClientItem(string serverId, SeriesDto series, LibraryType libraryType,
        string reason)
    {
        var progress = series.Pages <= 0 ? 0 : Math.Clamp((decimal) series.PagesRead / series.Pages, 0, 1);
        return new ClientMediaItemDto($"{serverId}:series:{series.Id}", series.Id, series.Name ?? string.Empty, series.LibraryId,
            series.LibraryName, libraryType, series.Pages, series.PagesRead, decimal.Round(progress, 4),
            $"/api/image/series-cover?seriesId={series.Id}", $"/library/{series.LibraryId}/series/{series.Id}", reason);
    }
}
