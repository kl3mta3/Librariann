using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Metadata;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

[Authorize(Policy = PolicyGroups.ManageMetadataPolicy)]
public sealed class MetadataLookupController(IMetadataLookupService metadataLookupService,
    IMetadataApplicationService metadataApplicationService) : BaseApiController
{
    [HttpPost("search")]
    public async Task<ActionResult<MetadataLookupResponse>> Search([FromBody] MetadataLookupRequest request,
        CancellationToken cancellationToken) => Ok(await metadataLookupService.SearchAsync(UserId, request, cancellationToken));

    [HttpPost("apply")]
    public async Task<ActionResult<ApplyMetadataResponse>> Apply([FromBody] ApplyMetadataRequest request,
        CancellationToken cancellationToken) =>
        Ok(await metadataApplicationService.ApplyAsync(UserId, request, cancellationToken));
}
