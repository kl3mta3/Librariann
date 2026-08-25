using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Acquisition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

/// <summary>
/// Admin/elevated-user configuration surface for indexers, download clients, and other providers.
/// Credential inputs are write-only and never returned by these endpoints.
/// </summary>
[Authorize(Policy = PolicyGroups.ManageAcquisitionPolicy)]
public sealed class IntegrationProviderController(
    IIntegrationProviderService providerService,
    IIntegrationProviderTestService providerTestService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IntegrationProviderDto>>> GetAll(CancellationToken ct) =>
        Ok(await providerService.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<IntegrationProviderDto>> Create(UpsertIntegrationProviderDto dto, CancellationToken ct) =>
        Ok(await providerService.CreateAsync(dto, ct));

    [HttpPut]
    public async Task<ActionResult<IntegrationProviderDto>> Update(UpsertIntegrationProviderDto dto, CancellationToken ct) =>
        Ok(await providerService.UpdateAsync(dto, ct));

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] int id, CancellationToken ct)
    {
        await providerService.DeleteAsync(id, ct);
        return Ok();
    }

    [HttpPost("test")]
    public async Task<ActionResult<ProviderTestResult>> Test([FromQuery] int id, CancellationToken ct) =>
        Ok(await providerTestService.TestAsync(id, ct));
}
