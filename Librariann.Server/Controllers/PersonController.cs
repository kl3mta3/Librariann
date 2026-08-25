using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Librariann.Models.Mapping;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.Metadata;
using Librariann.API.Services.SignalR;
using Librariann.Common.Extensions;
using Librariann.Common.Helpers;
using Librariann.Models.Constants;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Filtering.v2.Requests;
using Librariann.Models.DTOs.Metadata.Browse;
using Librariann.Models.DTOs.Person;
using Librariann.Models.DTOs.Recommendation;
using Librariann.Models.DTOs.SignalR;
using Librariann.Models.Entities.Enums;
using Librariann.Server.Attributes;
using Librariann.Server.Extensions;
using Librariann.Services.Plus;
using Librariann.Services.Scanner;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;
public class PersonController(
    IUnitOfWork unitOfWork,
    ILocalizationService localizationService,
    ICoverDbService coverDbService,
    IImageService imageService,
    IEventHub eventHub,
    IPersonService personService,
    IAuthorMetadataService authorMetadataService)
    : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PersonDto>> GetPersonByName(string name)
    {
        var person = await unitOfWork.PersonRepository.GetPersonDtoByName(name, UserId);
        if (person == null) return NotFound();

        person.Roles = (await unitOfWork.PersonRepository.GetRolesForPersonByName(person.Id, UserId)).ToList();
        person.IsFollowed = await unitOfWork.PersonRepository.IsFollowingAsync(UserId, person.Id,
            HttpContext.RequestAborted);

        EnrichWithWebLinks(person);

        return Ok(person);
    }

    [PersonAccess]
    [HttpPost("follow")]
    public async Task<ActionResult<bool>> SetFollowed([FromQuery] int personId, [FromQuery] bool followed)
    {
        var existing = await unitOfWork.PersonRepository.GetFollowAsync(UserId, personId,
            HttpContext.RequestAborted);
        if (followed && existing is null)
            unitOfWork.PersonRepository.Follow(new Librariann.Models.Entities.User.AppUserFollowedPerson
            {
                AppUserId = UserId,
                PersonId = personId,
            });
        else if (!followed && existing is not null)
            unitOfWork.PersonRepository.Unfollow(existing);

        if (unitOfWork.HasChanges()) await unitOfWork.CommitAsync(HttpContext.RequestAborted);
        return Ok(followed);
    }

    /// <summary>
    /// Populate <see cref="PersonDto.WebLinks"/> from set ids
    /// </summary>
    /// <param name="personDto"></param>
    /// <remarks><see cref="PersonDto.Roles"/> must be set for this to work</remarks>
    private static void EnrichWithWebLinks(PersonDto personDto)
    {
        if (personDto.Roles == null) return;

        var isCharacter = personDto.Roles.Count == 1 && personDto.Roles.Contains(PersonRole.Character);
        personDto.WebLinks = [];

        if (personDto.AniListId != 0)
        {
            var urlPrefix = isCharacter ? ScrobblingService.AniListCharacterWebsite : ScrobblingService.AniListStaffWebsite;
            personDto.WebLinks.Add($"{urlPrefix}{personDto.AniListId}");
        }

        if (personDto.MalId != 0)
        {
            var urlPrefix = isCharacter ? ScrobblingService.MalCharacterWebsite : ScrobblingService.MalStaffWebsite;
            personDto.WebLinks.Add($"{urlPrefix}{personDto.MalId}");
        }

        // Hardcover currently does not seem to have characters
        if (!string.IsNullOrEmpty(personDto.HardcoverId) && !isCharacter)
        {
            personDto.WebLinks.Add($"{ScrobblingService.HardcoverStaffWebsite}{personDto.HardcoverId}");
        }

        if (!string.IsNullOrWhiteSpace(personDto.OpenLibraryId) && !isCharacter)
        {
            personDto.WebLinks.Add($"https://openlibrary.org/authors/{Uri.EscapeDataString(personDto.OpenLibraryId)}");
        }
    }

    /// <summary>
    /// Find a person by name or alias against a query string
    /// </summary>
    /// <param name="queryString"></param>
    /// <returns></returns>
    [HttpGet("search")]
    public async Task<ActionResult<List<PersonDto>>> SearchPeople([FromQuery] string queryString)
    {
        return Ok(await unitOfWork.PersonRepository.SearchPeople(queryString));
    }

    /// <summary>
    /// Returns all roles for a Person
    /// </summary>
    /// <param name="personId"></param>
    /// <returns></returns>
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<PersonRole>>> GetRolesForPersonByName(int personId)
    {
        return Ok(await unitOfWork.PersonRepository.GetRolesForPersonByName(personId, UserId));
    }


    /// <summary>
    /// Returns a list of authors and artists for browsing
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="userParams"></param>
    /// <returns></returns>
    [HttpPost("all")]
    public async Task<ActionResult<PagedList<BrowsePersonDto>>> GetPeopleForBrowse(PersonFilterDto filter, [FromQuery] UserParams? userParams)
    {
        userParams ??= UserParams.Default;

        var list = await unitOfWork.PersonRepository.GetBrowsePersonDtos(UserId, filter, userParams);
        Response.AddPaginationHeader(list.CurrentPage, list.PageSize, list.TotalCount, list.TotalPages);

        return Ok(list);
    }

    /// <summary>
    /// Updates the Person
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update")]
    [Authorize(PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<PersonDto>> UpdatePerson(UpdatePersonDto dto)
    {
        // This needs to get all people and update them equally
        var person = await unitOfWork.PersonRepository.GetPersonById(dto.Id, PersonIncludes.Aliases);
        if (person == null) return BadRequest(await localizationService.TranslateAsync(UserId, "person-doesnt-exist"));

        if (string.IsNullOrEmpty(dto.Name)) return BadRequest(await localizationService.TranslateAsync(UserId, "person-name-required"));


        // Validate the name is unique
        if (dto.Name != person.Name && !(await unitOfWork.PersonRepository.IsNameUnique(dto.Name)))
        {
            return BadRequest(await localizationService.TranslateAsync(UserId, "person-name-unique"));
        }

        // Update name first, in case it got moved to aliases
        person.Name = dto.Name.Trim();
        person.NormalizedName = person.Name.ToNormalized();

        var success = await personService.UpdatePersonAliasesAsync(person, dto.Aliases);
        if (!success) return BadRequest(await localizationService.TranslateAsync(UserId, "aliases-have-overlap"));


        person.Description = dto.Description ?? string.Empty;
        person.CoverImageLocked = dto.CoverImageLocked;

        if (dto.MalId is > 0)
        {
            person.MalId = (long) dto.MalId;
        }
        if (dto.AniListId is > 0)
        {
            person.AniListId = (int) dto.AniListId;
        }

        if (!string.IsNullOrEmpty(dto.HardcoverId?.Trim()))
        {
            person.HardcoverId = dto.HardcoverId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.OpenLibraryId))
        {
            person.OpenLibraryId = dto.OpenLibraryId.Trim();
        }

        var asin = dto.Asin?.Trim();
        if (!string.IsNullOrEmpty(asin) && Parser.IsLikelyValidAsin(asin))
        {
            person.Asin = asin;
        }

        unitOfWork.PersonRepository.Update(person);
        await unitOfWork.CommitAsync();

        return Ok(person.ToPersonDto());
    }

    [HttpGet("metadata-search")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<IReadOnlyCollection<AuthorMetadataCandidateDto>>> SearchAuthorMetadata(
        [FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("An author name is required.");
        return Ok(await authorMetadataService.SearchAsync(query, HttpContext.RequestAborted));
    }

    [HttpPost("metadata-apply")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<PersonDto>> ApplyAuthorMetadata(ApplyAuthorMetadataDto dto)
    {
        var person = await unitOfWork.PersonRepository.GetPersonById(dto.PersonId, PersonIncludes.Aliases);
        if (person is null) return NotFound();

        var details = await authorMetadataService.GetDetailsAsync(dto.ProviderKey, dto.ExternalId,
            HttpContext.RequestAborted);
        if (details is null) return NotFound("The selected author metadata no longer exists.");

        var existingMatch = await unitOfWork.PersonRepository.GetPersonByOpenLibraryId(details.ExternalId,
            HttpContext.RequestAborted);
        if (existingMatch is not null && existingMatch.Id != person.Id)
            return Conflict($"This Open Library author is already matched to {existingMatch.Name}.");

        person.OpenLibraryId = details.ExternalId;
        if ((dto.OverwriteExisting || string.IsNullOrWhiteSpace(person.Description)) &&
            !string.IsNullOrWhiteSpace(details.Description))
            person.Description = details.Description;

        var aliases = person.Aliases.Select(alias => alias.Alias)
            .Concat(details.Aliases)
            .Where(alias => !alias.Equals(person.Name, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        // Never silently merge identities when a provider alias belongs to another local person.
        await personService.UpdatePersonAliasesAsync(person, aliases);

        if (!person.CoverImageLocked &&
            (dto.OverwriteExisting || string.IsNullOrWhiteSpace(person.CoverImage)) &&
            !string.IsNullOrWhiteSpace(details.PortraitUrl))
        {
            var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync();
            var portrait = await imageService.CreateThumbnailFromUrl(details.PortraitUrl,
                Librariann.Services.ImageService.GetPersonFormat(person.Id), settings.EncodeMediaAs, 400, 400);
            if (!string.IsNullOrWhiteSpace(portrait))
            {
                person.CoverImage = portrait;
                imageService.UpdateColorScape(person);
            }
        }

        unitOfWork.PersonRepository.Update(person);
        await unitOfWork.CommitAsync(HttpContext.RequestAborted);
        await eventHub.SendMessageAsync(MessageFactory.CoverUpdate,
            MessageFactory.CoverUpdateEvent(person.Id, "person"), false);

        var result = person.ToPersonDto();
        result.Roles = (await unitOfWork.PersonRepository.GetRolesForPersonByName(person.Id, UserId)).ToList();
        result.IsFollowed = await unitOfWork.PersonRepository.IsFollowingAsync(UserId, person.Id,
            HttpContext.RequestAborted);
        EnrichWithWebLinks(result);
        return Ok(result);
    }

    [HttpPost("metadata-refresh-all")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public ActionResult RefreshAllAuthorMetadata()
    {
        BackgroundJob.Enqueue<IAuthorMetadataRefreshService>(service => service.RefreshAllAsync());
        return Accepted();
    }

    /// <summary>
    /// Attempts to download the cover from CoversDB (Note: Not yet release in Librariann)
    /// </summary>
    /// <param name="personId"></param>
    /// <returns></returns>
    [PersonAccess]
    [HttpPost("fetch-cover")]
    public async Task<ActionResult<string>> DownloadCoverImage([FromQuery] int personId)
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync();
        var person = await unitOfWork.PersonRepository.GetPersonById(personId);
        if (person == null) return BadRequest(await localizationService.TranslateAsync(UserId, "person-doesnt-exist"));

        var personImage = await coverDbService.DownloadPersonImageAsync(person, settings.EncodeMediaAs);

        if (string.IsNullOrEmpty(personImage))
        {

            return BadRequest(await localizationService.TranslateAsync(UserId, "person-image-doesnt-exist"));
        }

        person.CoverImage = personImage;
        imageService.UpdateColorScape(person);
        unitOfWork.PersonRepository.Update(person);

        await unitOfWork.CommitAsync();
        await eventHub.SendMessageAsync(MessageFactory.CoverUpdate, MessageFactory.CoverUpdateEvent(person.Id, "person"), false);

        return Ok(personImage);
    }

    /// <summary>
    /// Returns the top 20 series that the "person" is known for. This will use Average Rating when applicable (Librariann+ field), else it's a random sort
    /// </summary>
    /// <param name="personId"></param>
    /// <returns></returns>
    [PersonAccess]
    [HttpGet("series-known-for")]
    public async Task<ActionResult<IEnumerable<SeriesDto>>> GetKnownSeries(int personId)
    {
        return Ok(await unitOfWork.PersonRepository.GetSeriesKnownFor(personId, UserId));
    }


    /// <summary>
    /// Returns all individual chapters by role. Limited to 20 results.
    /// </summary>
    /// <param name="personId"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    [PersonAccess]
    [HttpGet("chapters-by-role")]
    public async Task<ActionResult<IEnumerable<StandaloneChapterDto>>> GetChaptersByRole(int personId, PersonRole role)
    {
        return Ok(await unitOfWork.PersonRepository.GetChaptersForPersonByRole(personId, UserId, role));
    }

    /// <summary>
    /// Merges Persons into one, this action is irreversible
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("merge")]
    [Authorize(PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<PersonDto>> MergePeople(PersonMergeDto dto)
    {
        var dst = await unitOfWork.PersonRepository.GetPersonById(dto.DestId, PersonIncludes.All);
        if (dst == null) return BadRequest();

        var src = await unitOfWork.PersonRepository.GetPersonById(dto.SrcId, PersonIncludes.All);
        if (src == null) return BadRequest();

        await personService.MergePeopleAsync(src, dst);
        await eventHub.SendMessageAsync(MessageFactory.PersonMerged, MessageFactory.PersonMergedMessage(dst, src));

        return Ok(dst.ToPersonDto());
    }

    /// <summary>
    /// Ensure the alias is valid to be added. For example, the alias cannot be on another person or be the same as the current person name/alias.
    /// </summary>
    /// <param name="dto">alias check request</param>
    /// <returns></returns>
    [HttpPost("valid-alias")]
    public async Task<ActionResult<bool>> IsValidAlias(PersonAliasCheckDto dto)
    {
        var person = await unitOfWork.PersonRepository.GetPersonById(dto.PersonId, PersonIncludes.Aliases);
        if (person == null) return NotFound();

        var aliasIsName = dto.Name.ToNormalized() == dto.Alias.ToNormalized();
        var existingAlias = await unitOfWork.PersonRepository.AnyAliasExist(dto.Alias);

        return Ok(!existingAlias && !aliasIsName);
    }


}
