using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.Metadata;
using Librariann.API.Services.SignalR;
using Librariann.Common.Extensions;
using Librariann.Models.DTOs.SignalR;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Person;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Metadata;

public sealed class AuthorMetadataRefreshService(
    IUnitOfWork unitOfWork,
    IAuthorMetadataService authorMetadataService,
    IPersonService personService,
    IImageService imageService,
    IEventHub eventHub,
    ILogger<AuthorMetadataRefreshService> logger) : IAuthorMetadataRefreshService
{
    public async Task RefreshAllAsync()
    {
        var ct = CancellationToken.None;
        var people = await unitOfWork.PersonRepository.GetAllPeople(PersonIncludes.All, ct);
        var writers = people.Where(IsWriter).ToArray();
        var claimedIds = people
            .Select(person => person.OpenLibraryId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        var refreshed = 0;
        var matched = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var person in writers)
        {
            try
            {
                AuthorMetadataDetails? details;
                if (!string.IsNullOrWhiteSpace(person.OpenLibraryId))
                {
                    details = await authorMetadataService.GetDetailsAsync("open-library", person.OpenLibraryId, ct);
                    if (details is null)
                    {
                        skipped++;
                        continue;
                    }
                    refreshed++;
                }
                else
                {
                    var candidates = await authorMetadataService.SearchAsync(person.Name, ct);
                    var exact = candidates
                        .Where(candidate => candidate.MatchScore == 100 &&
                                            candidate.Name.ToNormalized() == person.NormalizedName)
                        .Where(candidate => !claimedIds.Contains(candidate.ExternalId))
                        .OrderByDescending(candidate => candidate.PortraitUri is not null)
                        .ThenByDescending(candidate => candidate.WorkCount)
                        .ToArray();
                    if (exact.Length == 0)
                    {
                        skipped++;
                        continue;
                    }

                    details = await authorMetadataService.GetDetailsAsync(exact[0].ProviderKey,
                        exact[0].ExternalId, ct);
                    if (details is null || claimedIds.Contains(details.ExternalId))
                    {
                        skipped++;
                        continue;
                    }

                    person.OpenLibraryId = details.ExternalId;
                    claimedIds.Add(details.ExternalId);
                    matched++;
                }

                if (string.IsNullOrWhiteSpace(person.Description) && !string.IsNullOrWhiteSpace(details.Description))
                    person.Description = details.Description;

                var aliases = person.Aliases.Select(alias => alias.Alias)
                    .Concat(details.Aliases)
                    .Where(alias => !alias.Equals(person.Name, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                await personService.UpdatePersonAliasesAsync(person, aliases, ct);

                var coverChanged = false;
                if (!person.CoverImageLocked && string.IsNullOrWhiteSpace(person.CoverImage) &&
                    !string.IsNullOrWhiteSpace(details.PortraitUrl))
                {
                    var portrait = await imageService.CreateThumbnailFromUrl(details.PortraitUrl,
                        ImageService.GetPersonFormat(person.Id), settings.EncodeMediaAs, 400, 400);
                    if (!string.IsNullOrWhiteSpace(portrait))
                    {
                        person.CoverImage = portrait;
                        imageService.UpdateColorScape(person);
                        coverChanged = true;
                    }
                }

                unitOfWork.PersonRepository.Update(person);
                await unitOfWork.CommitAsync(ct);
                if (coverChanged)
                    await eventHub.SendMessageAsync(MessageFactory.CoverUpdate,
                        MessageFactory.CoverUpdateEvent(person.Id, "person"), false);

                // Be respectful of Open Library's public service during large refreshes.
                await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "Unable to refresh Open Library metadata for author {AuthorName}", person.Name);
                await unitOfWork.RollbackAsync(ct);
            }
        }

        logger.LogInformation(
            "Author metadata refresh complete: {Matched} matched, {Refreshed} refreshed, {Skipped} require review, {Failed} failed out of {Total}",
            matched, refreshed, skipped, failed, writers.Length);
    }

    private static bool IsWriter(Person person) =>
        person.SeriesMetadataPeople.Any(link => link.Role == PersonRole.Writer) ||
        person.ChapterPeople.Any(link => link.Role == PersonRole.Writer);
}
