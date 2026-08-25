using System;
using System.Security.Cryptography;
using Librariann.API.Services;
using Microsoft.AspNetCore.DataProtection;

namespace Librariann.Services;

/// <summary>
/// Central protection boundary for recoverable integration credentials.
/// Persistence code must call this service before writing a secret to storage.
/// </summary>
public sealed class CredentialProtectionService(IDataProtectionProvider dataProtectionProvider)
    : ICredentialProtectionService
{
    internal const string ProtectedValuePrefix = "librariann:credential:v1:";
    private const string ProtectorPurpose = "Librariann.Credentials.v1";

    public string Protect(string plaintext, string scope)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        ValidateScope(scope);

        var protector = dataProtectionProvider.CreateProtector(ProtectorPurpose, scope);
        return ProtectedValuePrefix + protector.Protect(plaintext);
    }

    public string Unprotect(string protectedValue, string scope)
    {
        ArgumentException.ThrowIfNullOrEmpty(protectedValue);
        ValidateScope(scope);

        if (!IsProtected(protectedValue))
        {
            throw new CryptographicException("The stored credential is not protected.");
        }

        var protector = dataProtectionProvider.CreateProtector(ProtectorPurpose, scope);
        return protector.Unprotect(protectedValue[ProtectedValuePrefix.Length..]);
    }

    public bool IsProtected(string value)
    {
        return !string.IsNullOrEmpty(value) &&
               value.StartsWith(ProtectedValuePrefix, StringComparison.Ordinal);
    }

    private static void ValidateScope(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
    }
}
