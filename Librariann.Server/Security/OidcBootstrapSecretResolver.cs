using System;
using System.IO;
using Librariann.API.Services;
using Librariann.Common;
using Librariann.Services;
using Microsoft.AspNetCore.DataProtection;

namespace Librariann.Server.Security;

/// <summary>
/// Resolves the OIDC secret before the application service provider exists. The protected value uses the same
/// persisted ASP.NET Data Protection key ring and credential format as server-setting secrets.
/// </summary>
internal static class OidcBootstrapSecretResolver
{
    internal const string EnvironmentVariable = "LIBRARIANN_OIDC_CLIENT_SECRET";

    internal sealed record Resolution(
        Configuration.OpenIdConnectSettings RuntimeSettings,
        Configuration.OpenIdConnectSettings? SettingsToPersist);

    internal static Resolution Resolve(Configuration.OpenIdConnectSettings settings,
        string dataProtectionDirectory, string? environmentSecret = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataProtectionDirectory);

        if (!string.IsNullOrWhiteSpace(environmentSecret))
        {
            return new Resolution(Clone(settings, environmentSecret), null);
        }

        if (string.IsNullOrEmpty(settings.Secret))
        {
            return new Resolution(Clone(settings, string.Empty), null);
        }

        Directory.CreateDirectory(dataProtectionDirectory);
        var provider = DataProtectionProvider.Create(new DirectoryInfo(dataProtectionDirectory), builder =>
            builder.SetApplicationName("Librariann"));
        var credentials = new CredentialProtectionService(provider);

        if (credentials.IsProtected(settings.Secret))
        {
            var plaintext = credentials.Unprotect(settings.Secret,
                ServerSettingCredentialScopes.OidcClientSecret);
            return new Resolution(Clone(settings, plaintext), null);
        }

        var protectedSecret = credentials.Protect(settings.Secret,
            ServerSettingCredentialScopes.OidcClientSecret);
        return new Resolution(Clone(settings, settings.Secret), Clone(settings, protectedSecret));
    }

    private static Configuration.OpenIdConnectSettings Clone(Configuration.OpenIdConnectSettings settings,
        string secret) => new()
    {
        Authority = settings.Authority,
        ClientId = settings.ClientId,
        Secret = secret,
        CustomScopes = [.. settings.CustomScopes],
    };
}
