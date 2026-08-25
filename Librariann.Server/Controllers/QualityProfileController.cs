using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Acquisition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

public sealed class QualityProfileController(IQualityProfileService qualityProfileService) : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = PolicyGroups.SearchIndexersPolicy)]
    public async Task<ActionResult<IReadOnlyCollection<QualityProfileDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await qualityProfileService.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = PolicyGroups.ManageAcquisitionPolicy)]
    public async Task<ActionResult<QualityProfileDto>> Upsert([FromBody] UpsertQualityProfileRequest request,
        CancellationToken cancellationToken) => Ok(await qualityProfileService.UpsertAsync(request, cancellationToken));

    [HttpDelete]
    [Authorize(Policy = PolicyGroups.ManageAcquisitionPolicy)]
    public async Task<IActionResult> Delete([FromQuery] int id, CancellationToken cancellationToken)
    {
        await qualityProfileService.DeleteAsync(id, cancellationToken);
        return Ok();
    }
}
