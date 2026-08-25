using System;
using System.Globalization;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace Librariann.Server.Middleware;

public class AuthenticationRateLimiterPolicy : IRateLimiterPolicy<string>
{
    private const int PermitLimit = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var remoteAddress = httpContext.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown";
        var endpoint = httpContext.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        return RateLimitPartition.GetFixedWindowLimiter($"{endpoint}:{remoteAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = PermitLimit,
                QueueLimit = 0,
                Window = Window,
            });
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; } =
        (context, _) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int) retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return new ValueTask();
        };
}
