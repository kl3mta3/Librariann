using Librariann.Models.Entities.Acquisition;

namespace Librariann.Services.Acquisition;

internal static class IntegrationCredentialScope
{
    public static string For(IntegrationProviderConfiguration configuration, string field) =>
        $"integration-provider:{configuration.CredentialKey:N}:{field}";
}

