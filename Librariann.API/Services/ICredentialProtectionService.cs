namespace Librariann.API.Services;

/// <summary>
/// Protects credentials that must be recovered to call an external service.
/// User login passwords do not use this service; ASP.NET Identity stores those
/// as one-way password hashes.
/// </summary>
public interface ICredentialProtectionService
{
    /// <summary>
    /// Encrypts a credential for storage. Scope must identify the owning record
    /// and field, for example <c>download-client:42:password</c>.
    /// </summary>
    string Protect(string plaintext, string scope);

    /// <summary>
    /// Decrypts a value previously protected for the exact same scope.
    /// Plaintext values are rejected rather than silently accepted.
    /// </summary>
    string Unprotect(string protectedValue, string scope);

    bool IsProtected(string value);
}

/// <summary>
/// Stable purpose scopes for inherited server-setting credentials. Changing these values makes existing ciphertext
/// unreadable, so additions should be append-only and versioned.
/// </summary>
public static class ServerSettingCredentialScopes
{
    public const string MaskedValue = "************";
    public const string SmtpPassword = "server-setting:smtp:password:v1";
    public const string OidcClientSecret = "server-setting:oidc:client-secret:v1";

    public static string ExternalSourceApiKey(int userId) =>
        $"user:{userId}:external-source:api-key:v1";
}
