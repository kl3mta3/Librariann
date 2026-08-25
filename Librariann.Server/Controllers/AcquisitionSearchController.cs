using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Acquisition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

/// <summary>
/// Provider-neutral interactive release search. Rejected results remain visible with explanations.
/// </summary>
[Authorize(Policy = PolicyGroups.SearchIndexersPolicy)]
public sealed class AcquisitionSearchController(IInteractiveSearchService searchService) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<InteractiveSearchResponse>> Search(InteractiveSearchRequest request,
        CancellationToken cancellationToken) => Ok(await searchService.SearchAsync(UserId, request, cancellationToken));
}
