using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Hangfire;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.DTOs.Person;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Metadata;
using Librariann.Services.Helpers;
using Librariann.Services.Scanner;

namespace Librariann.Services.Metadata;

public sealed class MetadataApplicationService(
    IUnitOfWork unitOfWork,
    IMetadataApplyTokenStore tokenStore,
    IMetadataProvenanceService provenanceService,
    ICoverDbService coverDbService) : IMetadataApplicationService
{
    private static readonly IReadOnlySet<MetadataFieldKey> SupportedFields = new HashSet<MetadataFieldKey>
    {
        MetadataFieldKey.Description,
        MetadataFieldKey.Cover,
        MetadataFieldKey.PublicationDate,
        MetadataFieldKey.Language,
        MetadataFieldKey.Authors,
        MetadataFieldKey.Publisher,
        MetadataFieldKey.Genres,
        MetadataFieldKey.WebLinks,
    };

    public async Task<ApplyMetadataResponse> ApplyAsync(int userId, ApplyMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        var series = await unitOfWork.SeriesRepository.GetSeriesByIdAsync(request.SeriesId,
                         SeriesIncludes.Metadata | SeriesIncludes.Library, cancellationToken)
                     ?? throw new LibrariannException("metadata-target-does-not-exist");
        if (!await unitOfWork.UserRepository.HasAccessToLibrary(userId, series.LibraryId, cancellationToken))
            throw new LibrariannException("metadata-target-is-not-accessible");
        if (!tokenStore.TryTake(userId, request.ApplyToken, out var candidate) || candidate is null)
            throw new LibrariannException("metadata-apply-token-invalid-or-expired");
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId, ct: cancellationToken);
        var mayViewAdult = user?.AgeRestriction is AgeRating.NotApplicable or >= AgeRating.AdultsOnly;
        if (candidate.IsAdult && !mayViewAdult)
            throw new LibrariannException("metadata-adult-content-not-allowed");
        if (!IsCompatible(series.Library.Type, candidate.MediaType))
            throw new LibrariannException("metadata-candidate-media-type-does-not-match-library");

        var fields = request.Fields.Distinct().ToArray();
        if (fields.Length == 0 || fields.Any(field => !SupportedFields.Contains(field)))
            throw new LibrariannException("metadata-apply-fields-invalid");

        var results = new List<MetadataFieldApplyResult>(fields.Length);
        foreach (var field in fields)
        {
            var permission = await provenanceService.CanRefreshAsync(MetadataEntityType.Series, series.Id, field,
                candidate.ProviderKey, cancellationToken);
            if (!permission.CanRefresh)
            {
                results.Add(new MetadataFieldApplyResult(field, false, permission.Reason));
                continue;
            }

            var canonicalValue = await ApplyFieldAsync(series, candidate, field, cancellationToken);
            if (canonicalValue is null)
            {
                results.Add(new MetadataFieldApplyResult(field, false, "The provider did not return a usable value."));
                continue;
            }

            await provenanceService.StageAsync(new RecordMetadataProvenanceRequest
            {
                EntityType = MetadataEntityType.Series,
                EntityId = series.Id,
                Field = field,
                ProviderKey = candidate.ProviderKey,
                ProviderItemId = candidate.ExternalId,
                CanonicalValue = canonicalValue,
            }, cancellationToken);
            results.Add(new MetadataFieldApplyResult(field, true, "Applied and protected from scanner overwrite."));
        }

        if (results.Any(result => result.Applied))
        {
            series.LastModifiedUtc = DateTime.UtcNow;
            series.LastModified = DateTime.Now;
            await unitOfWork.CommitAsync(cancellationToken);
            if ((await unitOfWork.SettingsRepository.GetSettingsDtoAsync(cancellationToken)).WriteMetadataToFiles)
            {
                var appliedFields = results.Where(result => result.Applied).Select(result => result.Field).ToHashSet();
                var fileUpdate = new MetadataFileUpdate
                {
                    Description = appliedFields.Contains(MetadataFieldKey.Description) ? candidate.Description.Trim() : null,
                    PublicationYear = appliedFields.Contains(MetadataFieldKey.PublicationDate)
                        ? candidate.PublicationYear
                        : null,
                    Language = appliedFields.Contains(MetadataFieldKey.Language)
                        ? candidate.Languages.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
                        : null,
                    Authors = appliedFields.Contains(MetadataFieldKey.Authors) ? Clean(candidate.Authors) : null,
                    Genres = appliedFields.Contains(MetadataFieldKey.Genres) ? Clean(candidate.Genres) : null,
                    Publisher = appliedFields.Contains(MetadataFieldKey.Publisher)
                        ? candidate.Publishers.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
                        : null,
                };
                BackgroundJob.Enqueue<IMetadataFileWriteJobService>(service =>
                    service.WriteSeriesFilesAsync(series.Id, fileUpdate, CancellationToken.None));
            }
        }

        return new ApplyMetadataResponse(series.Id, candidate.ProviderKey, candidate.ExternalId, results);
    }

    private async Task<string?> ApplyFieldAsync(Librariann.Models.Entities.Series series,
        NormalizedMetadataCandidate candidate, MetadataFieldKey field, CancellationToken cancellationToken)
    {
        var metadata = series.Metadata;
        switch (field)
        {
            case MetadataFieldKey.Description:
                var description = candidate.Description.Trim();
                if (description.Length == 0) return null;
                metadata.Summary = description;
                metadata.SummaryLocked = true;
                return description;
            case MetadataFieldKey.Cover:
                if (candidate.CoverUri is null || candidate.CoverUri.Scheme != Uri.UriSchemeHttps) return null;
                await coverDbService.SetSeriesCoverByUrl(series, candidate.CoverUri.ToString(), fromBase64: false,
                    chooseBetterImage: false, ct: cancellationToken);
                return candidate.CoverUri.ToString();
            case MetadataFieldKey.PublicationDate:
                if (candidate.PublicationYear is not (>= 1 and <= 9999)) return null;
                metadata.ReleaseYear = candidate.PublicationYear.Value;
                metadata.ReleaseYearLocked = true;
                return candidate.PublicationYear.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case MetadataFieldKey.Language:
                var language = candidate.Languages.FirstOrDefault(value =>
                    !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 35)?.Trim();
                if (language is null) return null;
                metadata.Language = language;
                metadata.LanguageLocked = true;
                return language;
            case MetadataFieldKey.Authors:
                var authors = Clean(candidate.Authors);
                if (authors.Count == 0) return null;
                await SeriesService.HandlePeopleUpdateAsync(metadata,
                    authors.Select(name => new PersonDto {Name = name}).ToArray(), PersonRole.Writer, unitOfWork);
                metadata.WriterLocked = true;
                return string.Join('|', authors.Order(StringComparer.OrdinalIgnoreCase));
            case MetadataFieldKey.Publisher:
                var publisher = candidate.Publishers.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
                if (publisher is null) return null;
                await SeriesService.HandlePeopleUpdateAsync(metadata,
                    [new PersonDto {Name = publisher}], PersonRole.Publisher, unitOfWork);
                metadata.PublisherLocked = true;
                return publisher;
            case MetadataFieldKey.Genres:
                var genres = Clean(candidate.Genres);
                if (genres.Count == 0) return null;
                var existingGenres = await unitOfWork.GenreRepository.GetAllGenresByNamesAsync(
                    genres.Select(Parser.Normalize), cancellationToken);
                TagHelper.UpdateTagList(genres.ToList(), metadata.Genres, existingGenres.ToArray(),
                    genre => metadata.Genres.Add(genre), () => metadata.GenresLocked = true);
                return string.Join('|', genres.Order(StringComparer.OrdinalIgnoreCase));
            case MetadataFieldKey.WebLinks:
                if (candidate.DetailsUri is null || candidate.DetailsUri.Scheme != Uri.UriSchemeHttps) return null;
                var links = metadata.WebLinks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Append(candidate.DetailsUri.ToString())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(20)
                    .ToArray();
                metadata.WebLinks = string.Join(',', links);
                return metadata.WebLinks;
            default:
                return null;
        }
    }

    private static IReadOnlyCollection<string> Clean(IEnumerable<string> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToArray();

    private static bool IsCompatible(LibraryType libraryType, LibrariannMediaType mediaType) => mediaType switch
    {
        // Audiobooks have no dedicated free metadata provider (no keyless Audible API) - reuse Book-sourced
        // metadata (author, cover, description, etc.) since it's the same underlying bibliographic data.
        LibrariannMediaType.Book => libraryType is LibraryType.Book or LibraryType.LightNovel or LibraryType.Audiobook,
        LibrariannMediaType.Comic => libraryType is LibraryType.Comic or LibraryType.ComicVine,
        LibrariannMediaType.Manga => libraryType is LibraryType.Manga,
        _ => false,
    };
}
