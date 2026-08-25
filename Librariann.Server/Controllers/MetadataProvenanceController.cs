using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Metadata;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

[Authorize(Policy = PolicyGroups.ManageMetadataPolicy)]
public sealed class MetadataProvenanceController(IMetadataProvenanceService provenanceService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MetadataProvenanceDto>>> GetAll(
        [FromQuery] MetadataEntityType entityType, [FromQuery] int entityId, CancellationToken cancellationToken) =>
        Ok(await provenanceService.GetAllAsync(entityType, entityId, cancellationToken));

    [HttpGet("can-refresh")]
    public async Task<ActionResult<MetadataRefreshPermission>> CanRefresh([FromQuery] MetadataEntityType entityType,
        [FromQuery] int entityId, [FromQuery] MetadataFieldKey field, [FromQuery] string providerKey,
        CancellationToken cancellationToken) => Ok(await provenanceService.CanRefreshAsync(entityType, entityId, field,
        providerKey, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<MetadataProvenanceDto>> Record([FromBody] RecordMetadataProvenanceRequest request,
        CancellationToken cancellationToken) => Ok(await provenanceService.RecordAsync(request, cancellationToken));
}
