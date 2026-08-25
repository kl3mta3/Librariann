using Librariann.API.Services;
using Librariann.API.Store;
using Librariann.Common;
using Librariann.Database.Extensions;
using Librariann.Models.Constants;
using Librariann.Server.Logging;
using Librariann.Server.Middleware;
using Librariann.Server.Store;
using Librariann.Services.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Librariann.Server.Extensions;


public static class ApplicationServiceExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        services.AddScoped<ILoggingService, LoggingService>();
        services.AddSingleton<IClientInfoAccessor, ClientInfoAccessor>();
        services.AddScoped<UserContext>();
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<UserContext>());

        services.AddLibrariannDatabases();
        services.AddLibrariannServices();

        services.AddSignalR(opt => opt.EnableDetailedErrors = true);

        services.AddEasyCaching(options =>
        {
            options.UseInMemory(EasyCacheProfiles.Favicon);
            options.UseInMemory(EasyCacheProfiles.Publisher);
            options.UseInMemory(EasyCacheProfiles.Library);
            options.UseInMemory(EasyCacheProfiles.RevokedJwt);
            options.UseInMemory(EasyCacheProfiles.LocaleOptions);

            // LibrariannPlus stuff
            options.UseInMemory(EasyCacheProfiles.LibrariannPlusExternalSeries);
            options.UseInMemory(EasyCacheProfiles.License);
            options.UseInMemory(EasyCacheProfiles.LicenseInfo);
            options.UseInMemory(EasyCacheProfiles.LibrariannPlusMatchSeries);
            options.UseInMemory(EasyCacheProfiles.ProviderHealth);
        });

        services.AddMemoryCache(options =>
        {
            options.SizeLimit = Configuration.CacheSize * 1024 * 1024; // 75 MB
            options.CompactionPercentage = 0.1; // LRU compaction, Evict 10% when limit reached
        });

        services.AddSingleton<TicketSerializer>();
        services.AddSingleton<ITicketStore, CustomTicketStore>();
    }
}
