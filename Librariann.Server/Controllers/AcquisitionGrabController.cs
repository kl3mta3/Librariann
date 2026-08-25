using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Acquisition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

[Authorize(Policy = PolicyGroups.GrabReleasesPolicy)]
public sealed class AcquisitionGrabController(IReleaseGrabService grabService) : BaseApiController
{
    [HttpGet("clients")]
    public async Task<ActionResult<System.Collections.Generic.IReadOnlyCollection<DownloadClientOption>>> GetClients(CancellationToken cancellationToken) =>
        Ok(await grabService.GetAvailableClientsAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<GrabReleaseResponse>> Grab(GrabReleaseRequest request, CancellationToken cancellationToken) =>
        Ok(await grabService.GrabAsync(UserId, request, cancellationToken));
}
