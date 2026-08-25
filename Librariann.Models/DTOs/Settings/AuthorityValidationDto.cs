using System.ComponentModel;

namespace Librariann.Models.DTOs.Settings;

public sealed record AuthorityValidationDto(string Authority);

public enum AuthorityValidationResult
{
    /// <summary>
    /// Librariann can load the OIDC configuration and the issuer matches
    /// </summary>
    [Description("Success")]
    Success = 0,
    /// <summary>
    /// Librariann can load the OIDC configuration, but the issuer does not match
    /// </summary>
    [Description("InvalidAuthority")]
    InvalidAuthority = 1,
    /// <summary>
    /// Librariann cannot load the OIDC configuration
    /// </summary>
    [Description("Failure")]
    Failure = 2,
    /// <summary>
    /// Librariann cannot validate the authority because it is not configured
    /// </summary>
    [Description("NotApplicable")]
    NotApplicable = 3,
    /// <summary>
    /// The authority is missing https
    /// </summary>
    [Description("MissingHttps")]
    MissingHttps = 4,
}
