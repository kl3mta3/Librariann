using System.Collections.Generic;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

public class ActivityController(IUnitOfWork unitOfWork) : BaseApiController
{
    /// <summary>
    /// Returns active reading sessions on the Server
    /// </summary>
    /// <returns></returns>
    [Authorize(PolicyGroups.AdminPolicy)]
    [HttpGet("current")]
    public async Task<ActionResult<List<ReadingSessionDto>>> GetActiveReadingSessions()
    {
        return Ok(await unitOfWork.ReadingSessionRepository.GetAllReadingSessionAsync());
    }
}
