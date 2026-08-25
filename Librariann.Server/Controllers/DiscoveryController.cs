using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.Acquisition;
using Librariann.API.Services.Metadata;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.DTOs.Search;
using Librariann.Models.Entities.Enums;
using Librariann.Services.Scanner;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

/// <summary>
/// Unified discovery around a title rather than around a particular provider or indexer.
/// </summary>
public sealed class DiscoveryController(
    IUnitOfWork unitOfWork,
    ILocalizationService localizationService,
    IMetadataLookupService metadataLookupService,
    IInteractiveSearchService interactiveSearchService,
    IQualityProfileService qualityProfileService) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<UnifiedDiscoveryResponse>> Discover(
        [FromBody] UnifiedDiscoveryRequest request, CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        var libraries = await unitOfWork.LibraryRepository.GetLibraryIdsForUserIdAsync(UserId, QueryContext.Search);
        if (libraries.Count == 0)
            return BadRequest(await localizationService.TranslateAsync(UserId, "libraries-restricted"));

        var searchDto = SearchDto.FromQuery(query, false);
        if (!searchDto.HasShortcode) searchDto.Query = Parser.CleanQuery(query);
        var isAdmin = UserContext.HasRole(PolicyConstants.AdminRole);
        var libraryResults = await unitOfWork.SeriesRepository.SearchSeriesAsync(UserId, isAdmin, libraries,
            searchDto, cancellationToken);

        var user = isAdmin ? null : await unitOfWork.UserRepository.GetUserByIdAsync(UserId, ct: cancellationToken);
        // External catalogs and release feeds do not all provide consistent ratings. Adult-tier and unrestricted
        // profiles may use them; younger audience profiles fail closed instead of receiving unclassifiable results.
        var adultEligible = isAdmin || user?.AgeRestriction is AgeRating.NotApplicable or >= AgeRating.AdultsOnly;
        var restrictedByContentPolicy = !adultEligible;
        var canSearchMetadata = !restrictedByContentPolicy && UserContext.HasAnyRole(PolicyConstants.AdminRole,
            PolicyConstants.ManageMetadataRole);
        var canSearchReleases = !restrictedByContentPolicy && UserContext.HasAnyRole(PolicyConstants.AdminRole,
            PolicyConstants.SearchIndexersRole);

        MetadataLookupResponse? metadataResults = null;
        if (canSearchMetadata)
        {
            metadataResults = await metadataLookupService.SearchAsync(UserId, new MetadataLookupRequest
            {
                MediaType = request.MediaType,
                Title = query,
                Author = request.Author.Trim(),
                Isbn = request.Isbn.Trim(),
                Language = request.Language.Trim(),
                IncludeAdult = request.IncludeAdult,
            }, cancellationToken);
        }

        InteractiveSearchResponse? releaseResults = null;
        int? qualityProfileId = null;
        if (canSearchReleases)
        {
            qualityProfileId = request.QualityProfileId;
            if (!qualityProfileId.HasValue)
            {
                qualityProfileId = (await qualityProfileService.GetAllAsync(cancellationToken))
                    .FirstOrDefault(profile => profile.MediaType == request.MediaType)?.Id;
            }

            if (qualityProfileId.HasValue)
            {
                releaseResults = await interactiveSearchService.SearchAsync(UserId, new InteractiveSearchRequest
                {
                    QualityProfileId = qualityProfileId.Value,
                    Search = new IndexerSearchRequest
                    {
                        Title = query,
                        Author = request.Author.Trim(),
                        Isbn = request.Isbn.Trim(),
                    },
                    Evaluation = new ReleaseEvaluationContext
                    {
                        ExpectedTitle = query,
                        ExpectedAuthor = request.Author.Trim(),
                        OwnedFormat = request.OwnedFormat,
                    },
                }, cancellationToken);
            }
        }

        return Ok(new UnifiedDiscoveryResponse(
            libraryResults,
            metadataResults,
            releaseResults,
            new UnifiedDiscoveryAccess(true, canSearchMetadata, canSearchReleases, restrictedByContentPolicy),
            qualityProfileId));
    }
}
