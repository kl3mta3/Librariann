using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Acquisition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

[Authorize(Policy = PolicyGroups.ManageAcquisitionPolicy)]
public sealed class AcquisitionQueueController(IAcquisitionQueueService queueService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AcquisitionDownloadDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await queueService.GetAllAsync(cancellationToken));

    [HttpPost("poll")]
    public async Task<IActionResult> Poll(CancellationToken cancellationToken)
    {
        await queueService.PollAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("{downloadId:int}/retry")]
    public async Task<IActionResult> Retry(int downloadId, CancellationToken cancellationToken)
    {
        await queueService.RetryAsync(downloadId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{downloadId:int}/remove")]
    public async Task<IActionResult> Remove(int downloadId, RemoveAcquisitionDownloadRequest request,
        CancellationToken cancellationToken)
    {
        await queueService.RemoveAsync(downloadId, request.DeleteData, cancellationToken);
        return NoContent();
    }
}
