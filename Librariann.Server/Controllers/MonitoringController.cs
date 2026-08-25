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
public sealed class MonitoringController(IMonitoringService monitoringService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MonitoringTargetDto>>> GetAll(
        CancellationToken cancellationToken) => Ok(await monitoringService.GetAllAsync(cancellationToken));

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyCollection<MonitoringSearchRunDto>>> GetHistory(
        [FromQuery] int? targetId, [FromQuery] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(await monitoringService.GetHistoryAsync(targetId, take, cancellationToken));

    [HttpGet("wanted")]
    public async Task<ActionResult<IReadOnlyCollection<WantedItemDto>>> GetWanted(
        [FromQuery] int? targetId, CancellationToken cancellationToken = default) =>
        Ok(await monitoringService.GetWantedAsync(targetId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<MonitoringTargetDto>> Upsert(UpsertMonitoringTargetRequest request,
        CancellationToken cancellationToken) => Ok(await monitoringService.UpsertAsync(UserId, request, cancellationToken));

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] int id, CancellationToken cancellationToken)
    {
        await monitoringService.DeleteAsync(id, cancellationToken);
        return Ok();
    }

    [HttpPost("search-now")]
    public async Task<IActionResult> SearchNow([FromQuery] int id, CancellationToken cancellationToken)
    {
        await monitoringService.SearchNowAsync(id, cancellationToken);
        return Accepted();
    }

    [HttpPost("sync-catalog")]
    public async Task<IActionResult> SyncCatalog([FromQuery] int id, CancellationToken cancellationToken)
    {
        await monitoringService.SyncCatalogNowAsync(id, cancellationToken);
        return Accepted();
    }
}
