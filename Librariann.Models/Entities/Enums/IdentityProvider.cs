using System.ComponentModel;

namespace Librariann.Models.Entities.Enums;

/// <summary>
/// Who provides the identity of the user
/// </summary>
public enum IdentityProvider
{
    [Description("Librariann")]
    Librariann = 0,
    [Description("OpenID Connect")]
    OpenIdConnect = 1,
}
