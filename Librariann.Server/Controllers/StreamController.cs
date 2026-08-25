using System.Collections.Generic;
using System.Threading.Tasks;
using Librariann.API.Attributes;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Dashboard;
using Librariann.Models.DTOs.SideNav;
using Librariann.Server.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;
/// <summary>
/// Responsible for anything that deals with Streams (SmartFilters, ExternalSource, DashboardStream, SideNavStream)
/// </summary>
public class StreamController(
    IStreamService streamService,
    IUnitOfWork unitOfWork)
    : BaseApiController
{
    /// <summary>
    /// Returns the layout of the user's dashboard
    /// </summary>
    /// <returns></returns>
    [HttpGet("dashboard")]
    public async Task<ActionResult<IEnumerable<DashboardStreamDto>>> GetDashboardLayout(bool visibleOnly = true)
    {
        return Ok(await streamService.GetDashboardStreams(UserId, visibleOnly));
    }

    /// <summary>
    /// Return's the user's side nav
    /// </summary>
    [HttpGet("sidenav")]
    public async Task<ActionResult<IEnumerable<SideNavStreamDto>>> GetSideNav(bool visibleOnly = true)
    {
        return Ok(await streamService.GetSidenavStreams(UserId, visibleOnly));
    }

    /// <summary>
    /// Consumes a short-lived, one-use linked-library launch reference. The opaque token contains no credential and
    /// expires after two minutes; the recoverable API key remains server-side until this redirect is consumed.
    /// </summary>
    [HttpGet("open-external-source")]
    [AllowAnonymous]
    [SkipDeviceTracking]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult OpenExternalSource([FromQuery] string token,
        [FromServices] IExternalSourceLaunchTokenStore tokenStore)
    {
        if (!tokenStore.TryTake(token, out var destination) || destination == null) return NotFound();
        return Redirect(destination.AbsoluteUri);
    }

    /// <summary>
    /// Issues a fresh short-lived launch reference for a linked library owned by the current user.
    /// </summary>
    [HttpGet("external-source-launch")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult> GetExternalSourceLaunch([FromQuery] int externalSourceId)
    {
        var path = await streamService.CreateExternalSourceLaunchAsync(UserId, externalSourceId,
            HttpContext.RequestAborted);
        return Content(path, "text/plain");
    }

    /// <summary>
    /// Return's the user's external sources
    /// </summary>
    [HttpGet("external-sources")]
    public async Task<ActionResult<IEnumerable<ExternalSourceDto>>> GetExternalSources()
    {
        return Ok(await streamService.GetExternalSources(UserId));
    }

    /// <summary>
    /// Create an external Source
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("create-external-source")]
    public async Task<ActionResult<ExternalSourceDto>> CreateExternalSource(ExternalSourceDto dto)
    {
        // Check if a host and api key exists for the current user
        return Ok(await streamService.CreateExternalSource(UserId, dto));
    }

    /// <summary>
    /// Updates an existing external source
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update-external-source")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<ExternalSourceDto>> UpdateExternalSource(ExternalSourceDto dto)
    {
        // Check if a host and api key exists for the current user
        return Ok(await streamService.UpdateExternalSource(UserId, dto));
    }

    /// <summary>
    /// Validates the external source by host is unique (for this user)
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("external-source-exists")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<bool>> ExternalSourceExists(ExternalSourceDto dto)
    {
        return Ok(await unitOfWork.AppUserExternalSourceRepository.ExternalSourceExists(UserId, dto.Name, dto.Host, dto.ApiKey));
    }

    /// <summary>
    /// Delete's the external source
    /// </summary>
    /// <param name="externalSourceId"></param>
    /// <returns></returns>
    [HttpDelete("delete-external-source")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> ExternalSourceExists(int externalSourceId)
    {
        await streamService.DeleteExternalSource(UserId, externalSourceId);
        return Ok();
    }


    /// <summary>
    /// Creates a Dashboard Stream from a SmartFilter and adds it to the user's dashboard as visible
    /// </summary>
    /// <param name="smartFilterId"></param>
    /// <returns></returns>
    [HttpPost("add-dashboard-stream")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<DashboardStreamDto>> AddDashboard([FromQuery] int smartFilterId)
    {
        return Ok(await streamService.CreateDashboardStreamFromSmartFilter(UserId, smartFilterId));
    }

    /// <summary>
    /// Updates the visibility of a dashboard stream
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update-dashboard-stream")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> UpdateDashboardStream(DashboardStreamDto dto)
    {
        await streamService.UpdateDashboardStream(UserId, dto);
        return Ok();
    }

    /// <summary>
    /// Updates the position of a dashboard stream
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update-dashboard-position")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> UpdateDashboardStreamPosition(UpdateStreamPositionDto dto)
    {
        await streamService.UpdateDashboardStreamPosition(UserId, dto);
        return Ok();
    }


    /// <summary>
    /// Creates a SideNav Stream from a SmartFilter and adds it to the user's sidenav as visible
    /// </summary>
    /// <param name="smartFilterId"></param>
    /// <returns></returns>
    [HttpPost("add-sidenav-stream")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<SideNavStreamDto>> AddSideNav([FromQuery] int smartFilterId)
    {
        return Ok(await streamService.CreateSideNavStreamFromSmartFilter(UserId, smartFilterId));
    }

    /// <summary>
    /// Creates a SideNav Stream from a SmartFilter and adds it to the user's sidenav as visible
    /// </summary>
    /// <param name="externalSourceId"></param>
    /// <returns></returns>
    [HttpPost("add-sidenav-stream-from-external-source")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<SideNavStreamDto>> AddSideNavFromExternalSource([FromQuery] int externalSourceId)
    {
        return Ok(await streamService.CreateSideNavStreamFromExternalSource(UserId, externalSourceId));
    }

    /// <summary>
    /// Updates the visibility of a dashboard stream
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update-sidenav-stream")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> UpdateSideNavStream(SideNavStreamDto dto)
    {
        await streamService.UpdateSideNavStream(UserId, dto);
        return Ok();
    }

    /// <summary>
    /// Updates the position of a dashboard stream
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update-sidenav-position")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> UpdateSideNavStreamPosition(UpdateStreamPositionDto dto)
    {
        await streamService.UpdateSideNavStreamPosition(UserId, dto);
        return Ok();
    }

    [HttpPost("bulk-sidenav-stream-visibility")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> BulkUpdateSideNavStream(BulkUpdateSideNavStreamVisibilityDto dto)
    {
        await streamService.UpdateSideNavStreamBulk(UserId, dto);
        return Ok();
    }

    /// <summary>
    /// Removes a Smart Filter from a user's SideNav Streams
    /// </summary>
    /// <param name="sideNavStreamId"></param>
    /// <returns></returns>
    [HttpDelete("smart-filter-side-nav-stream")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> DeleteSmartFilterSideNavStream([FromQuery] int sideNavStreamId)
    {
        await streamService.DeleteSideNavSmartFilterStream(UserId, sideNavStreamId);
        return Ok();
    }

    /// <summary>
    /// Removes a Smart Filter from a user's Dashboard Streams
    /// </summary>
    /// <param name="dashboardStreamId"></param>
    /// <returns></returns>
    [HttpDelete("smart-filter-dashboard-stream")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> DeleteSmartFilterDashboardStream([FromQuery] int dashboardStreamId)
    {
        await streamService.DeleteDashboardSmartFilterStream(UserId, dashboardStreamId);
        return Ok();
    }
}
