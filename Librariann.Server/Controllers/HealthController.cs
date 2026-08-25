using Librariann.API.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

[AllowAnonymous]
[SkipDeviceTracking]
public class HealthController : BaseApiController
{
    /// <summary>
    /// No-op method that just returns Ok. Used for health checks in Docker containers.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ActionResult<string> GetHealth()
    {
        return Ok("Ok");
    }
}
