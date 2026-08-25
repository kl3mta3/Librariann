using System;
using System.Threading.Tasks;
using Librariann.API.Services;
using Librariann.API.Store;
using Librariann.Models.Entities.User;
using Microsoft.AspNetCore.Http;

namespace Librariann.Server.Middleware;

/// <summary>
/// If the user is authenticated, will update the <see cref="AppUser.LastActive"/> field.
/// </summary>
/// <remarks>This should be last in the stack of middlewares</remarks>
/// <param name="next"></param>
public class UpdateUserAsActiveMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserContext userContext, IActiveUserTrackerService tracker)
    {
        try
        {
            var userId = userContext.GetUserId();
            if (userId > 0)
            {
                tracker.RecordActive(userId.Value);
            }
        }
        catch (Exception)
        {
            await next(context);
            return;
        }

        await next(context);
    }
}
