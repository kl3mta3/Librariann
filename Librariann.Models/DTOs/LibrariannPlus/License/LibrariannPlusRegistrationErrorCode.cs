using System.ComponentModel;

namespace Librariann.Models.DTOs.LibrariannPlus.License;

public enum LibrariannPlusRegistrationErrorCode
{
    [Description("None")]
    None = 0,
    [Description("Registration Failed")]
    RegistrationFailed = 1,
    [Description("Already Registered")]
    AlreadyRegistered = 2,
    [Description("Subscription Inactive")]
    SubscriptionInactive = 3,
    [Description("Internal Error")]
    InternalError = 4
}
