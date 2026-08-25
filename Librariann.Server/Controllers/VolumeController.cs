using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.SignalR;
using Librariann.Models.Constants;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.SignalR;
using Librariann.Server.Attributes;
using Librariann.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

public class VolumeController(IUnitOfWork unitOfWork, ILocalizationService localizationService, IEventHub eventHub)
    : BaseApiController
{
    /// <summary>
    /// Returns the appropriate Volume
    /// </summary>
    /// <param name="volumeId"></param>
    /// <returns></returns>
    [VolumeAccess]
    [HttpGet]
    public async Task<ActionResult<VolumeDto?>> GetVolume(int volumeId)
    {
        return Ok(await unitOfWork.VolumeRepository.GetVolumeDtoAsync(volumeId, UserId));
    }

    /// <summary>
    /// Updates the information on the Volume
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<VolumeDto>> UpdateVolume(UpdateVolumeDto dto)
    {
        var volume = await unitOfWork.VolumeRepository.GetVolumeByIdAsync(dto.Id);
        if (volume == null) return BadRequest(await localizationService.TranslateAsync(UserId, "volume-doesnt-exist"));

        ExternalMetadataIdHelper.SetExternalMetadataIds(volume, dto);

        unitOfWork.VolumeRepository.Update(volume);

        if (unitOfWork.HasChanges() && !await unitOfWork.CommitAsync())
            return BadRequest(await localizationService.TranslateAsync(UserId, "generic-error"));

        return Ok(await unitOfWork.VolumeRepository.GetVolumeDtoAsync(volume.Id, UserId));
    }

    /// <summary>
    /// Delete the Volume from the DB
    /// </summary>
    /// <param name="volumeId"></param>
    /// <returns></returns>
    [HttpDelete]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<bool>> DeleteVolume(int volumeId)
    {
        var volume = await unitOfWork.VolumeRepository.GetVolumeByIdAsync(volumeId,
            VolumeIncludes.Chapters | VolumeIncludes.People | VolumeIncludes.Tags);
        if (volume == null)
            return BadRequest(await localizationService.TranslateAsync(UserId, "volume-doesnt-exist"));

        unitOfWork.VolumeRepository.Remove(volume);

        if (await unitOfWork.CommitAsync())
        {
            await eventHub.SendMessageAsync(MessageFactory.VolumeRemoved, MessageFactory.VolumeRemovedEvent(volume.Id, volume.SeriesId), false);
            return Ok(true);
        }

        return Ok(false);
    }

    /// <summary>
    /// Delete multiple Volumes from the DB
    /// </summary>
    /// <param name="volumesIds"></param>
    /// <returns></returns>
    [HttpPost("multiple")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<bool>> DeleteMultipleVolumes(int[] volumesIds)
    {
        var volumes = await unitOfWork.VolumeRepository.GetVolumesById(volumesIds);
        if (volumes.Count != volumesIds.Length)
        {
            return BadRequest(await localizationService.TranslateAsync(UserId, "volume-doesnt-exist"));
        }

        unitOfWork.VolumeRepository.Remove(volumes);

        if (!await unitOfWork.CommitAsync())
        {
            return Ok(false);
        }

        foreach (var volume in volumes)
        {
            await eventHub.SendMessageAsync(MessageFactory.VolumeRemoved, MessageFactory.VolumeRemovedEvent(volume.Id, volume.SeriesId), false);
        }

        return Ok(true);
    }
}
