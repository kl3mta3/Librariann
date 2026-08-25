using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for the old self-mapping AutoMapper profiles (<c>CreateMap&lt;T, T&gt;()</c>) used to clone
/// default dashboard/side-nav stream templates onto a newly seeded user without sharing references with
/// <see cref="Librariann.Models.Defaults"/>.
/// </summary>
public static class UserStreamMapping
{
    public static AppUserDashboardStream Clone(this AppUserDashboardStream s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        IsProvided = s.IsProvided,
        Order = s.Order,
        StreamType = s.StreamType,
        Visible = s.Visible,
        SmartFilter = s.SmartFilter,
        AppUserId = s.AppUserId,
        AppUser = s.AppUser,
    };

    public static AppUserSideNavStream Clone(this AppUserSideNavStream s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        IsProvided = s.IsProvided,
        Order = s.Order,
        LibraryId = s.LibraryId,
        ExternalSourceId = s.ExternalSourceId,
        StreamType = s.StreamType,
        Visible = s.Visible,
        SmartFilter = s.SmartFilter,
        AppUserId = s.AppUserId,
        AppUser = s.AppUser,
    };
}
