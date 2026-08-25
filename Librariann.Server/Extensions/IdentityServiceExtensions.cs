using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.API.Services;
using Librariann.Common;
using Librariann.Common.EnvironmentInfo;
using Librariann.Database;
using Librariann.Models.Constants;
using Librariann.Models.Entities.Acquisition;
using Librariann.Models.Entities.Progress;
using Librariann.Models.Entities.User;
using Librariann.Server.Helpers;
using Librariann.Server.Middleware;
using Librariann.Server.Security;
using Librariann.Services;
using Librariann.Services.Acquisition;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using MessageReceivedContext = Microsoft.AspNetCore.Authentication.JwtBearer.MessageReceivedContext;

namespace Librariann.Server.Extensions;

public static class IdentityServiceExtensions
{
    private const string DynamicHybrid = nameof(DynamicHybrid);
    public const string OpenIdConnect = nameof(OpenIdConnect);
    private const string LocalIdentity = nameof(LocalIdentity);

    private const string OidcCallback = "/signin-oidc";
    private const string OidcLogoutCallback = "/signout-callback-oidc";

    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment environment)
    {
        services.Configure<IdentityOptions>(options =>
        {
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/";
            ConfigurePasswordPolicy(options.Password);
        });

        services.AddIdentityCore<AppUser>(opt =>
            {
                ConfigurePasswordPolicy(opt.Password);

                opt.SignIn.RequireConfirmedEmail = false;

                opt.Lockout.AllowedForNewUsers = true;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                opt.Lockout.MaxFailedAccessAttempts = 5;

            })
            .AddTokenProvider<DataProtectorTokenProvider<AppUser>>(TokenOptions.DefaultProvider)
            .AddTokenProvider<RefreshTokenProvider>(TokenService.RefreshTokenProviderName)
            .AddRoles<AppRole>()
            .AddRoleManager<RoleManager<AppRole>>()
            .AddSignInManager<SignInManager<AppUser>>()
            .AddRoleValidator<RoleValidator<AppRole>>()
            .AddEntityFrameworkStores<DataContext>();

        var oidcSettings = ResolveOidcBootstrapSettings();

        var auth = services.AddAuthentication(DynamicHybrid);
        var enableOidc = oidcSettings.Enabled && services.SetupOpenIdConnectAuthentication(auth, oidcSettings, environment);

        auth.AddPolicyScheme(DynamicHybrid, LocalIdentity, options =>
        {
            options.ForwardDefaultSelector = ctx =>
            {
                // Priority 1: Check for API/Auth Key
                var apiKey = AuthKeyAuthenticationHandler.ExtractAuthKey(ctx.Request);
                if (!string.IsNullOrEmpty(apiKey))
                {
                    return AuthKeyAuthenticationOptions.SchemeName;
                }

                // Priority 2: OIDC paths and cookies
                if (enableOidc)
                {
                    if (ctx.Request.Path.StartsWithSegments(OidcCallback) ||
                        ctx.Request.Path.StartsWithSegments(OidcLogoutCallback))
                    {
                        return OpenIdConnect;
                    }

                    if (ctx.Request.Cookies.ContainsKey(OidcService.CookieName))
                    {
                        return OpenIdConnect;
                    }
                }

                // Priority 3: JWT Bearer token
                if (ctx.Request.Headers.Authorization.Count != 0)
                {
                    return LocalIdentity;
                }

                // Default to JWT
                return LocalIdentity;
            };
        });

        auth.AddJwtBearer(LocalIdentity, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"]!)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = BuildInfo.JwtIssuer,
                ValidAudience = BuildInfo.JwtAudience,
                NameClaimType = JwtRegisteredClaimNames.Name,
                RoleClaimType = ClaimTypes.Role,
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = SetTokenFromQuery,
                OnTokenValidated = ctx =>
                {
                    (ctx.Principal?.Identity as ClaimsIdentity)?.AddClaim(new Claim("AuthType", nameof(AuthenticationType.JWT)));
                    return Task.CompletedTask;
                }
            };
        });

        // Add Bearer as an alias to LocalIdentity
        auth.AddPolicyScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.ForwardDefaultSelector = _ => LocalIdentity;
        });

        auth.AddScheme<AuthKeyAuthenticationOptions, AuthKeyAuthenticationHandler>(
            AuthKeyAuthenticationOptions.SchemeName,
            _ => { });


        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyGroups.AdminPolicy, policy => policy.RequireRole(PolicyConstants.AdminRole))
            .AddPolicy(PolicyGroups.DownloadPolicy,
                policy => policy.RequireRole(PolicyConstants.DownloadRole, PolicyConstants.AdminRole))
            .AddPolicy(PolicyGroups.ChangePasswordPolicy,
                policy => policy.RequireRole(PolicyConstants.ChangePasswordRole, PolicyConstants.AdminRole))
            .AddPolicy(PolicyGroups.BookmarkPolicy,
                policy => policy.RequireRole(PolicyConstants.BookmarkRole, PolicyConstants.AdminRole))
            .AddPolicy(PolicyGroups.SearchIndexersPolicy,
                policy => policy.RequireRole(PolicyConstants.SearchIndexersRole, PolicyConstants.AdminRole))
            .AddPolicy(PolicyGroups.GrabReleasesPolicy,
                policy => policy.RequireRole(PolicyConstants.GrabReleasesRole, PolicyConstants.AdminRole))
            .AddPolicy(PolicyGroups.ManageMetadataPolicy,
                policy => policy.RequireRole(PolicyConstants.ManageMetadataRole, PolicyConstants.AdminRole))
            .AddPolicy(PolicyGroups.ManageLibrariesPolicy,
                policy => policy.RequireRole(PolicyConstants.ManageLibrariesRole, PolicyConstants.AdminRole))
            .AddPolicy(PolicyGroups.ManageAcquisitionPolicy,
                policy => policy.RequireRole(PolicyConstants.ManageAcquisitionRole, PolicyConstants.AdminRole));

        return services;
    }

    private static Configuration.OpenIdConnectSettings ResolveOidcBootstrapSettings()
    {
        var settings = Configuration.OidcSettings;
        var keyDirectory = Path.Join(Directory.GetCurrentDirectory(), "config", "data-protection-keys");

        try
        {
            var resolution = OidcBootstrapSecretResolver.Resolve(settings, keyDirectory,
                Environment.GetEnvironmentVariable(OidcBootstrapSecretResolver.EnvironmentVariable));
            if (resolution.SettingsToPersist != null)
            {
                Configuration.OidcSettings = resolution.SettingsToPersist;
                Log.Information("Migrated the OIDC bootstrap client secret to protected storage");
            }

            return resolution.RuntimeSettings;
        }
        catch (Exception ex)
        {
            // A lost or mismatched key ring must disable OIDC rather than fall back to treating ciphertext as a secret.
            // Local JWT/auth-key login remains available so an administrator can repair the configuration.
            Log.Error(ex, "Unable to decrypt the OIDC bootstrap client secret; OIDC is disabled for this process");
            return new Configuration.OpenIdConnectSettings
            {
                Authority = settings.Authority,
                ClientId = settings.ClientId,
                Secret = string.Empty,
                CustomScopes = [.. settings.CustomScopes],
            };
        }
    }

    internal static void ConfigurePasswordPolicy(PasswordOptions options)
    {
        // Prefer long passphrases over composition rules. Existing password hashes
        // remain valid; this policy applies when a password is created or changed.
        options.RequireDigit = false;
        options.RequireLowercase = false;
        options.RequireUppercase = false;
        options.RequireNonAlphanumeric = false;
        options.RequiredLength = 12;
        options.RequiredUniqueChars = 1;
    }

    private static bool SetupOpenIdConnectAuthentication(this IServiceCollection services, AuthenticationBuilder auth,
        Configuration.OpenIdConnectSettings settings, IWebHostEnvironment environment)
    {
        var isDevelopment = environment.IsEnvironment(Environments.Development);
        var baseUrl = Configuration.BaseUrl;

        Uri authority;
        try
        {
            authority = new IntegrationEndpointValidator().ValidateAsync(settings.Authority,
                    Configuration.OidcPrivateNetworkEnabled)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OpenID Connect authority failed URL/network validation");
            return false;
        }

        if (!isDevelopment && authority.Scheme != Uri.UriSchemeHttps && !Configuration.OidcPrivateNetworkEnabled)
        {
            Log.Error("OpenIdConnect authority is not using https, you must configure tls for your idp.");
            return false;
        }

        var url = authority.AbsoluteUri.TrimEnd('/') + "/.well-known/openid-configuration";
        var oidcHttpClient = new IntegrationHttpClientFactory().Create(new IntegrationProviderConfiguration
        {
            BaseUrl = authority.AbsoluteUri,
            AllowPrivateNetwork = Configuration.OidcPrivateNetworkEnabled,
        });

        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            url,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(oidcHttpClient) { RequireHttps = authority.Scheme == Uri.UriSchemeHttps }
        );

        services.AddSingleton(configurationManager);

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme).Configure<ITicketStore>((options, store) =>
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;

            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.IsEssential = true;
            options.Cookie.MaxAge = TimeSpan.FromDays(7);
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.SessionStore = store;

            if (isDevelopment)
            {
                options.Cookie.Domain = null;
            }

            options.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = async ctx =>
                {
                    var oidcService = ctx.HttpContext.RequestServices.GetRequiredService<IOidcService>();
                    var user = await oidcService.RefreshCookieToken(ctx);

                    if (user != null)
                    {
                        var claims = await OidcService.ConstructNewClaimsList(ctx.HttpContext.RequestServices, ctx.Principal, user, false);
                        ctx.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(claims, ctx.Scheme.Name)));
                    }
                },
                OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
            };
        });

        auth.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
        auth.AddOpenIdConnect(OpenIdConnect, options =>
        {
            options.Authority = settings.Authority;
            options.ClientId = settings.ClientId;
            options.ClientSecret = settings.Secret;
            options.RequireHttpsMetadata = options.Authority.StartsWith("https://");
            // Keep discovery, token exchange, refresh, and user-info calls on the same DNS-rebinding-safe,
            // no-redirect backchannel. Setting only Authority would make the handler construct an unsafe client.
            options.ConfigurationManager = configurationManager;
            options.Backchannel = oidcHttpClient;

            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.CallbackPath = OidcCallback;
            options.SignedOutCallbackPath = OidcLogoutCallback;

            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            // Due to some (Authelia) OIDC providers, we need to map these claims explicitly. Such that no flow breaks in the
            // OidcService. Claims from the UserInfoEndPoint are not added automatically, we map some to the claim we need.
            // And copy all over down below
            options.MapInboundClaims = true;
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");

            options.Scope.Clear();
            foreach (var scope in GetValidScopes(configurationManager, settings))
            {
                options.Scope.Add(scope);
            }

            options.Events = new OpenIdConnectEventsHelper(baseUrl, isDevelopment);
        });

        return true;
    }

    private static IEnumerable<string> GetValidScopes(
        ConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        Configuration.OpenIdConnectSettings settings
    )
    {
        var scopes = OidcService.DefaultScopes;

        ICollection<string> supportedScopes;
        try
        {
            supportedScopes = configurationManager.GetConfigurationAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
                .ScopesSupported;
        }
        catch (Exception ex)
        {
            // Most idps will safely ignore invalid scopes (all except Authelia as far as I know), so we return them here
            // to have the least amount of impact on users
            Log.Error(ex, "Failed to load OIDC configuration, scopes will not be filtered. This may cause issues with some idps.");
            return scopes.Concat(settings.CustomScopes);
        }

        return scopes.Where(scope =>
        {
            if (supportedScopes.Contains(scope))
                return true;

            Log.Warning("Scope {Scope} is configured, but not supported by your OIDC provider. Skipping", scope);
            return false;
        }).Concat(settings.CustomScopes);
    }

    private static Task SetTokenFromQuery(MessageReceivedContext context)
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;

        // Only use query string based token on SignalR hubs
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    }
}
