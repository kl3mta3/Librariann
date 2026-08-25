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
public sealed class AcquisitionImportController(IAcquisitionImportService importService) : BaseApiController
{
    [HttpGet("destinations")]
    public async Task<ActionResult<IReadOnlyCollection<ImportDestinationOption>>> Destinations(CancellationToken cancellationToken) =>
        Ok(await importService.GetDestinationsAsync(cancellationToken));

    [HttpPost("analyze")]
    public async Task<ActionResult<ImportAnalysisResult>> Analyze([FromQuery] int downloadId, CancellationToken cancellationToken) =>
        Ok(await importService.AnalyzeAsync(downloadId, cancellationToken));

    [HttpPost("commit")]
    public async Task<ActionResult<CommitImportResult>> Commit([FromBody] CommitImportRequest request,
        CancellationToken cancellationToken) => Ok(await importService.CommitAsync(request, cancellationToken));
}
