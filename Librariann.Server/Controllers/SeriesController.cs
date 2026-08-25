using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.Plus;
using Librariann.Common;
using Librariann.Common.Extensions;
using Librariann.Common.Helpers;
using Librariann.Models.Constants;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Dashboard;
using Librariann.Models.DTOs.Filtering.v2;
using Librariann.Models.DTOs.Filtering.v2.Requests;
using Librariann.Models.DTOs.Metadata.Matching;
using Librariann.Models.DTOs.Recommendation;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.DTOs.SeriesDetail;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using Librariann.Models.Entities.MetadataMatching;
using Librariann.Models.Extensions;
using Librariann.Server.Attributes;
using Librariann.Server.Extensions;
using Librariann.Server.Helpers;
using Librariann.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Librariann.Server.Controllers;

public class SeriesController(
    ILogger<SeriesController> logger,
    ITaskScheduler taskScheduler,
    IUnitOfWork unitOfWork,
    ISeriesService seriesService,
    ILocalizationService localizationService,
    IExternalMetadataService externalMetadataService)
    : BaseApiController
{

    /// <summary>
    /// Gets series with the applied Filter
    /// </summary>
    /// <param name="userParams"></param>
    /// <param name="seriesFilterDto"></param>
    /// <returns></returns>
    [HttpPost("v2")]
    public async Task<ActionResult<PagedList<SeriesDto>>> GetSeriesForLibraryV2([FromQuery] UserParams userParams, [FromBody] SeriesFilterV2Dto seriesFilterDto)
    {
        var userId = UserId;
        var ct = HttpContext.RequestAborted;
        var series = await unitOfWork.SeriesRepository.GetSeriesDtoForLibraryIdAsync(userId, userParams, seriesFilterDto, ct: ct);

        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);

        return Ok(series);
    }

    /// <summary>
    /// Fetches a Series for a given Id
    /// </summary>
    /// <param name="seriesId">Series Id to fetch details for</param>
    /// <returns></returns>
    /// <exception cref="NoContent">Throws an exception if the series Id does exist</exception>
    [SeriesAccess]
    [HttpGet("{seriesId:int}")]
    public async Task<ActionResult<SeriesDto>> GetSeries(int seriesId)
    {
        var ct = HttpContext.RequestAborted;
        var series = await unitOfWork.SeriesRepository.GetSeriesDtoByIdAsync(seriesId, UserId, ct);
        if (series == null) return NotFound();
        return Ok(series);
    }

    /// <summary>
    /// Deletes a series from Librariann
    /// </summary>
    /// <param name="seriesId"></param>
    /// <returns>If the series was deleted or not</returns>
    [HttpDelete("{seriesId}")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<bool>> DeleteSeries(int seriesId)
    {
        var username = Username!;
        var ct = HttpContext.RequestAborted;
        logger.LogInformation("Series {SeriesId} is being deleted by {UserName}", seriesId, username.Sanitize());

        return Ok(await seriesService.DeleteMultipleSeries([seriesId], ct));
    }

    /// <summary>
    /// Deletes multiple series from Librariann at once
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("delete-multiple")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult> DeleteMultipleSeries(DeleteSeriesDto dto)
    {
        var username = Username!;
        var ct = HttpContext.RequestAborted;
        logger.LogInformation("Series {@SeriesId} is being deleted by {UserName}", dto.SeriesIds, username.Sanitize());

        if (await seriesService.DeleteMultipleSeries(dto.SeriesIds, ct)) return Ok(true);

        return BadRequest(await localizationService.TranslateAsync(UserId, "generic-series-delete"));
    }

    /// <summary>
    /// Returns All volumes for a series with progress information and Chapters
    /// </summary>
    /// <param name="seriesId"></param>
    /// <returns></returns>
    [SeriesAccess]
    [HttpGet("volumes")]
    public async Task<ActionResult<IEnumerable<VolumeDto>>> GetVolumes(int seriesId)
    {
        var ct = HttpContext.RequestAborted;
        return Ok(await unitOfWork.VolumeRepository.GetVolumesDtoAsync(seriesId, UserId, ct: ct));
    }

    /// <summary>
    /// Returns a single Volume with progress information and Chapters
    /// </summary>
    /// <param name="volumeId"></param>
    /// <returns></returns>
    [VolumeAccess]
    [HttpGet("volume")]
    public async Task<ActionResult<VolumeDto?>> GetVolume(int volumeId)
    {
        var ct = HttpContext.RequestAborted;
        var vol = await unitOfWork.VolumeRepository.GetVolumeDtoAsync(volumeId, UserId, ct);
        if (vol == null) return NotFound();
        return Ok(vol);
    }

    /// <summary>
    /// Returns a single Chapter with progress information
    /// </summary>
    /// <param name="chapterId"></param>
    /// <returns></returns>
    [ChapterAccess]
    [HttpGet("chapter")]
    public async Task<ActionResult<ChapterDto>> GetChapter(int chapterId)
    {
        var ct = HttpContext.RequestAborted;
        var chapter = await unitOfWork.ChapterRepository.GetChapterDtoAsync(chapterId, UserId, ct);
        if (chapter == null) return NotFound();

        return Ok(chapter);
    }

    /// <summary>
    /// Updates the Series
    /// </summary>
    /// <param name="updateSeries"></param>
    /// <returns>Updated Series</returns>
    [HttpPost("update")]
    [Authorize(Policy = PolicyGroups.ManageMetadataPolicy)]
    public async Task<ActionResult<SeriesDto>> UpdateSeries(UpdateSeriesDto updateSeries)
    {
        var ct = HttpContext.RequestAborted;
        var series = await unitOfWork.SeriesRepository.GetSeriesByIdAsync(updateSeries.Id,
            SeriesIncludes.Metadata | SeriesIncludes.Library, ct);
        if (series == null)
            return BadRequest(await localizationService.TranslateAsync(UserId, "series-doesnt-exist"));

        var renamed = false;
        var newName = updateSeries.Name?.Trim();
        if (!string.IsNullOrEmpty(newName) && newName != series.Name)
        {
            var normalizedNewName = newName.ToNormalized();
            if (!await unitOfWork.SeriesRepository.IsSeriesNameUniqueInLibraryAsync(
                    series.LibraryId, series.Format, normalizedNewName, series.Id, ct))
            {
                return BadRequest(await localizationService.TranslateAsync(UserId, "series-name-exists"));
            }

            // The current name may anchor merged folders on disk. Changing it would orphan those files and the scanner
            // would split them into a new series, so reject the edit.
            if (await externalMetadataService.WouldNameChangeOrphanMergedFiles(series, newName, ct))
            {
                return BadRequest(await localizationService.TranslateAsync(UserId, "series-name-orphans-files"));
            }

            series.Name = newName;
            // A user rename is an override; drop any prior K+ name override so K+ respects it
            series.Metadata.KPlusOverrides.Remove(MetadataSettingField.Name);
            renamed = true;
        }

        series.NormalizedName = series.Name.ToNormalized();

        if (renamed && !updateSeries.SortNameLocked)
        {
            // An unlocked sort name is derived from Name - reseed it (mirrors the scanner logic)
            series.SortName = series.Library is {RemovePrefixForSortName: true}
                ? BookSortTitlePrefixHelper.GetSortTitle(series.Name)
                : series.Name;
        }
        else if (!string.IsNullOrEmpty(updateSeries.SortName?.Trim()))
        {
            series.SortName = updateSeries.SortName.Trim();
        }

        var newLocalizedName = updateSeries.LocalizedName?.Trim();
        var newNormalizedLocalizedName = newLocalizedName.ToNormalized();
        if (series.NormalizedLocalizedName != newNormalizedLocalizedName)
        {
            // A localized name that collides (normalized) with another series' name in the library+format breaks the scanner
            if (!string.IsNullOrEmpty(newNormalizedLocalizedName) && !await unitOfWork.SeriesRepository.IsSeriesNameUniqueInLibraryAsync(
                    series.LibraryId, series.Format, newNormalizedLocalizedName, series.Id, ct))
            {
                return BadRequest(await localizationService.TranslateAsync(UserId, "series-localized-name-exists"));
            }

            // The current localized name may anchor merged folders on disk. Changing/clearing it would orphan those
            // files and the scanner would split them into a new series, so reject the edit.
            if (await externalMetadataService.WouldLocalizedNameChangeOrphanMergedFiles(series, newLocalizedName, ct))
            {
                return BadRequest(await localizationService.TranslateAsync(UserId, "series-localized-name-orphans-files"));
            }

            series.LocalizedName = newLocalizedName;
            series.NormalizedLocalizedName = newNormalizedLocalizedName;

            series.Metadata.KPlusOverrides.Remove(MetadataSettingField.LocalizedName);
        }

        series.NameLocked = updateSeries.NameLocked;
        series.SortNameLocked = updateSeries.SortNameLocked;
        series.LocalizedNameLocked = updateSeries.LocalizedNameLocked;

        ExternalMetadataIdHelper.SetExternalMetadataIds(series, updateSeries);

        var needsRefreshMetadata = false;
        // This is when you hit Reset
        if (series.CoverImageLocked && !updateSeries.CoverImageLocked)
        {
            // Trigger a refresh when we are moving from a locked image to a non-locked
            needsRefreshMetadata = true;
            series.CoverImage = null;
            series.CoverImageLocked = false;
            series.Metadata.KPlusOverrides.Remove(MetadataSettingField.Covers);
            logger.LogDebug("[SeriesCoverImageBug] Setting Series Cover Image to null: {SeriesId}", series.Id);
            series.ResetColorScape();

        }

        unitOfWork.SeriesRepository.Update(series);

        if (!await unitOfWork.CommitAsync(ct))
        {
            return BadRequest(await localizationService.TranslateAsync(UserId, "generic-series-update"));
        }

        // Pulls a fresh Series, must be after commit
        await externalMetadataService.UpdateSeriesMetadataProviderOverride(series.Id, updateSeries.MetadataProviderOverride, ct);

        if (needsRefreshMetadata)
        {
            await taskScheduler.RefreshSeriesMetadata(series.LibraryId, series.Id);
        }

        return Ok(await unitOfWork.SeriesRepository.GetSeriesDtoByIdAsync(series.Id, UserId, ct));
    }

    /// <summary>
    /// Gets all recently added series
    /// </summary>
    /// <param name="seriesFilterDto"></param>
    /// <param name="userParams"></param>
    /// <returns></returns>
    [HttpPost("recently-added-v2")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetRecentlyAddedV2(SeriesFilterV2Dto seriesFilterDto, [FromQuery] UserParams userParams)
    {
        var userId = UserId;
        var ct = HttpContext.RequestAborted;
        var series =
            await unitOfWork.SeriesRepository.GetRecentlyAddedAsync(userId, userParams, seriesFilterDto, ct);

        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);

        return Ok(series);
    }

    [HttpGet("recently-added-favorite-genres")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetRecentlyAddedInFavoriteGenres(
        [FromQuery] UserParams userParams)
    {
        var series = await unitOfWork.SeriesRepository.GetRecentlyAddedInFavoriteGenresAsync(UserId, userParams,
            HttpContext.RequestAborted);
        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);
        return Ok(series);
    }

    [HttpGet("recently-downloaded")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetRecentlyDownloaded([FromQuery] UserParams userParams)
    {
        var series = await unitOfWork.SeriesRepository.GetRecentlyDownloadedAsync(UserId, userParams,
            HttpContext.RequestAborted);
        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);
        return Ok(series);
    }

    [HttpGet("missing-from-monitored")]
    public async Task<ActionResult<IEnumerable<MissingSeriesItemDto>>> GetMissingFromMonitoredSeries(
        [FromQuery] UserParams userParams)
    {
        var items = await unitOfWork.SeriesRepository.GetMissingFromMonitoredSeriesAsync(UserId, userParams,
            HttpContext.RequestAborted);
        Response.AddPaginationHeader(items.CurrentPage, items.PageSize, items.TotalCount, items.TotalPages);
        return Ok(items);
    }

    [HttpGet("because-you-read")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetBecauseYouRead([FromQuery] UserParams userParams)
    {
        var series = await unitOfWork.SeriesRepository.GetBecauseYouReadAsync(UserId, userParams,
            HttpContext.RequestAborted);
        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);
        return Ok(series);
    }

    [HttpGet("new-from-followed-authors")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetNewFromFollowedAuthors(
        [FromQuery] UserParams userParams)
    {
        var series = await unitOfWork.SeriesRepository.GetNewFromFollowedAuthorsAsync(UserId, userParams,
            HttpContext.RequestAborted);
        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);
        return Ok(series);
    }

    [HttpGet("next-in-series")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetNextInSeries([FromQuery] UserParams userParams)
    {
        var series = await unitOfWork.SeriesRepository.GetNextInSeriesAsync(UserId, userParams,
            HttpContext.RequestAborted);
        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);
        return Ok(series);
    }

    /// <summary>
    /// Returns series that were recently updated, like adding or removing a chapter
    /// </summary>
    /// <param name="userParams">Page size and offset</param>
    /// <returns></returns>
    [HttpPost("recently-updated-series")]
    public async Task<ActionResult<IList<GroupedSeriesDto>>> GetRecentlyAddedChapters([FromQuery] UserParams? userParams)
    {
        userParams ??= UserParams.Default;
        var ct = HttpContext.RequestAborted;
        return Ok(await unitOfWork.SeriesRepository.GetRecentlyUpdatedSeriesAsync(UserId, userParams, ct));
    }

    /// <summary>
    /// Returns all series for the library
    /// </summary>
    /// <param name="seriesFilterDto"></param>
    /// <param name="userParams"></param>
    /// <param name="userId">Optional user id to request the OnDeck for someone else. They must have profile sharing enabled when doing so</param>
    /// <param name="context"></param>
    /// <returns></returns>
    [HttpPost("all-v2")]
    [ProfilePrivacy(allowMissingUserId: true)]
    public async Task<ActionResult<PagedList<SeriesDto>>> GetAllSeriesV2(SeriesFilterV2Dto seriesFilterDto, [FromQuery] UserParams userParams,
        [FromQuery] int? userId = null, [FromQuery] QueryContext context = QueryContext.None)
    {
        var ct = HttpContext.RequestAborted;
        var seriesForUser = userId ?? UserId;

        foreach (var stmt in await seriesService.GetProfilePrivacyStatements(seriesForUser, UserId, ct))
        {
            seriesFilterDto.Statements.Add(stmt);
        }

        var series = await unitOfWork.SeriesRepository.GetSeriesDtoForLibraryIdAsync(seriesForUser, userParams, seriesFilterDto, context, ct);

        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);

        return Ok(series);
    }


    /// <summary>
    /// Fetches series that are on deck aka have progress on them.
    /// </summary>
    /// <param name="userParams"></param>
    /// <param name="libraryId">Default of 0 meaning all libraries</param>
    /// <returns></returns>
    [HttpPost("on-deck")]
    public async Task<ActionResult<PagedList<SeriesDto>>> GetOnDeck([FromQuery] UserParams userParams, [FromQuery] int libraryId = 0)
    {
        var ct = HttpContext.RequestAborted;
        var pagedList = await unitOfWork.SeriesRepository.GetOnDeckAsync(UserId, libraryId, userParams, ct);

        Response.AddPaginationHeader(pagedList.CurrentPage, pagedList.PageSize, pagedList.TotalCount, pagedList.TotalPages);

        return Ok(pagedList);
    }


    /// <summary>
    /// Removes a series from displaying on deck until the next read event on that series
    /// </summary>
    /// <param name="seriesId"></param>
    /// <returns></returns>
    [HttpPost("remove-from-on-deck")]
    public async Task<ActionResult> RemoveFromOnDeck([FromQuery] int seriesId)
    {
        var ct = HttpContext.RequestAborted;
        await unitOfWork.SeriesRepository.RemoveFromOnDeckAsync(seriesId, UserId, ct);
        return Ok();
    }

    /// <summary>
    /// Get series a user is currently reading, requires the user to share their profile
    /// </summary>
    /// <param name="userParams"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    [ProfilePrivacy]
    [HttpGet("currently-reading")]
    public async Task<ActionResult<PagedList<SeriesDto>>> GetCurrentlyReadingForUser([FromQuery] UserParams userParams, [FromQuery] int userId)
    {
        var ct = HttpContext.RequestAborted;
        var pagedList = await seriesService.GetCurrentlyReading(userId, UserId, userParams, ct);

        Response.AddPaginationHeader(pagedList.CurrentPage, pagedList.PageSize, pagedList.TotalCount, pagedList.TotalPages);

        return Ok(pagedList);
    }


    /// <summary>
    /// Runs a Cover Image Generation task
    /// </summary>
    /// <param name="refreshSeriesDto"></param>
    /// <returns></returns>
    [HttpPost("refresh-metadata")]
    [Authorize(Policy = PolicyGroups.ManageMetadataPolicy)]
    public async Task<ActionResult> RefreshSeriesMetadata(RefreshSeriesDto refreshSeriesDto)
    {
        await taskScheduler.RefreshSeriesMetadata(refreshSeriesDto.LibraryId, refreshSeriesDto.SeriesId, refreshSeriesDto.ForceUpdate, refreshSeriesDto.ForceColorscape);
        return Ok();
    }

    /// <summary>
    /// Scan a series and force each file to be updated. This should be invoked via the User, hence why we force.
    /// </summary>
    /// <param name="refreshSeriesDto"></param>
    /// <returns></returns>
    [HttpPost("scan")]
    [Authorize(Policy = PolicyGroups.ManageLibrariesPolicy)]
    public ActionResult ScanSeries(RefreshSeriesDto refreshSeriesDto)
    {
        taskScheduler.ScanSeries(refreshSeriesDto.LibraryId, refreshSeriesDto.SeriesId, true);
        return Ok();
    }

    /// <summary>
    /// Run a file analysis on the series.
    /// </summary>
    /// <param name="refreshSeriesDto"></param>
    /// <returns></returns>
    [HttpPost("analyze")]
    [Authorize(Policy = PolicyGroups.ManageLibrariesPolicy)]
    public ActionResult AnalyzeSeries(RefreshSeriesDto refreshSeriesDto)
    {
        taskScheduler.AnalyzeFilesForSeries(refreshSeriesDto.LibraryId, refreshSeriesDto.SeriesId, refreshSeriesDto.ForceUpdate);
        return Ok();
    }

    /// <summary>
    /// Returns metadata for a given series
    /// </summary>
    /// <param name="seriesId"></param>
    /// <returns></returns>
    [SeriesAccess]
    [HttpGet("metadata")]
    public async Task<ActionResult<SeriesMetadataDto>> GetSeriesMetadata(int seriesId)
    {
        var ct = HttpContext.RequestAborted;
        return Ok(await unitOfWork.SeriesRepository.GetSeriesMetadataAsync(seriesId, ct));
    }

    /// <summary>
    /// Update series metadata
    /// </summary>
    /// <param name="updateSeriesMetadataDto"></param>
    /// <returns></returns>
    [HttpPost("metadata")]
    [Authorize(PolicyGroups.ManageMetadataPolicy)]
    public async Task<ActionResult> UpdateSeriesMetadata(UpdateSeriesMetadataDto updateSeriesMetadataDto)
    {
        var ct = HttpContext.RequestAborted;
        if (!await seriesService.UpdateSeriesMetadata(updateSeriesMetadataDto, ct))
            return BadRequest(await localizationService.TranslateAsync(UserId, "update-metadata-fail"));

        return Ok(await localizationService.TranslateAsync(UserId, "series-updated"));

    }

    /// <summary>
    /// Returns all Series grouped by the passed Collection Id with Pagination.
    /// </summary>
    /// <param name="collectionId">Collection Id to pull series from</param>
    /// <param name="userParams">Pagination information</param>
    /// <returns></returns>
    [HttpGet("series-by-collection")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetSeriesByCollectionTag(int collectionId, [FromQuery] UserParams userParams)
    {
        var ct = HttpContext.RequestAborted;
        var userId = UserId;
        var series =
            await unitOfWork.SeriesRepository.GetSeriesDtoForCollectionAsync(collectionId, userId, userParams, ct);

        Response.AddPaginationHeader(series.CurrentPage, series.PageSize, series.TotalCount, series.TotalPages);

        return Ok(series);
    }

    /// <summary>
    /// Fetches Series for a set of Ids. This will check User for permission access and filter out any Ids that don't exist or
    /// the user does not have access to.
    /// </summary>
    /// <returns></returns>
    [HttpPost("series-by-ids")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetAllSeriesById(SeriesByIdsDto dto)
    {
        var ct = HttpContext.RequestAborted;
        if (dto.SeriesIds == null) return BadRequest(await localizationService.TranslateAsync(UserId, "invalid-payload"));
        return Ok(await unitOfWork.SeriesRepository.GetSeriesDtoForIdsAsync(dto.SeriesIds, UserId, ct));
    }

    /// <summary>
    /// Get the age rating for the <see cref="AgeRating"/> enum value
    /// </summary>
    /// <param name="ageRating"></param>
    /// <returns></returns>
    [HttpGet("age-rating")]
    [ResponseCache(CacheProfileName = ResponseCacheProfiles.Month, VaryByQueryKeys = ["ageRating"])]
    public async Task<ActionResult<string>> GetAgeRating(int ageRating)
    {
        var ct = HttpContext.RequestAborted;
        var val = (AgeRating) ageRating;
        // NOTE: Why not rename NotApplicable to NoRestriction and avoid this extra if?
        if (val == AgeRating.NotApplicable)
            return await localizationService.TranslateAsync(UserId, "age-restriction-not-applicable");

        return Ok(val.ToDescription());
    }

    /// <summary>
    /// Get a special DTO for Series Detail page.
    /// </summary>
    /// <param name="seriesId"></param>
    /// <returns></returns>
    /// <remarks>Do not rely on this API externally. May change without hesitation. </remarks>
    [SeriesAccess]
    [HttpGet("series-detail")]
    public async Task<ActionResult<SeriesDetailDto>> GetSeriesDetailBreakdown(int seriesId)
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            return await seriesService.GetSeriesDetail(seriesId, UserId, ct);
        }
        catch (LibrariannException ex)
        {
            return BadRequest(await localizationService.TranslateAsync(UserId, ex.Message));
        }
    }



    /// <summary>
    /// Fetches the related series for a given series
    /// </summary>
    /// <param name="seriesId"></param>
    /// <param name="relation">Type of Relationship to pull back</param>
    /// <returns></returns>
    [SeriesAccess]
    [HttpGet("related")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetRelatedSeries(int seriesId, RelationKind relation)
    {
        var ct = HttpContext.RequestAborted;
        return Ok(await unitOfWork.SeriesRepository.GetSeriesForRelationKindAsync(UserId, seriesId, relation, ct));
    }

    /// <summary>
    /// Returns all related series against the passed series Id
    /// </summary>
    /// <param name="seriesId"></param>
    /// <returns></returns>
    [SeriesAccess]
    [HttpGet("all-related")]
    public async Task<ActionResult<RelatedSeriesDto>> GetAllRelatedSeries(int seriesId)
    {
        var ct = HttpContext.RequestAborted;
        return Ok(await seriesService.GetRelatedSeries(UserId, seriesId, ct));
    }


    /// <summary>
    /// Update the relations attached to the Series. Does not generate associated Sequel/Prequel pairs on target series.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update-related")]
    [Authorize(Policy = PolicyGroups.ManageMetadataPolicy)]
    public async Task<ActionResult> UpdateRelatedSeries(UpdateRelatedSeriesDto dto)
    {
        var ct = HttpContext.RequestAborted;
        if (await seriesService.UpdateRelatedSeries(dto, ct))
        {
            return Ok();
        }

        return BadRequest(await localizationService.TranslateAsync(UserId, "generic-relationship"));
    }

    /// <summary>
    /// Based on the delta times between when chapters are added, for series that are not Completed/Cancelled/Hiatus, forecast the next
    /// date when it will be available.
    /// </summary>
    /// <param name="seriesId"></param>
    /// <returns></returns>
    [SeriesAccess]
    [HttpGet("next-expected")]
    public async Task<ActionResult<NextExpectedChapterDto>> GetNextExpectedChapter(int seriesId)
    {
        var userId = UserId;
        var ct = HttpContext.RequestAborted;

        return Ok(await seriesService.GetEstimatedChapterCreationDate(seriesId, userId, ct));
    }

    /// <summary>
    /// Returns all Series that a user has access to
    /// </summary>
    /// <returns></returns>
    [HttpGet("series-with-annotations")]
    public async Task<ActionResult<IList<SeriesDto>>> GetSeriesWithAnnotations()
    {
        var ct = HttpContext.RequestAborted;
        return Ok(await unitOfWork.AnnotationRepository.GetSeriesWithAnnotations(UserId, ct));
    }


}
