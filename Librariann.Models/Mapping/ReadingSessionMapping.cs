using System.Linq;
using Librariann.Models.DTOs.Progress;
using Librariann.Models.Entities.Progress;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUserReadingSession, ReadingSessionDto&gt;()</c>.</summary>
public static class ReadingSessionMapping
{
    public static ReadingSessionDto ToReadingSessionDto(this AppUserReadingSession s) => new()
    {
        Id = s.Id,
        StartTimeUtc = s.StartTimeUtc,
        EndTimeUtc = s.EndTimeUtc,
        IsActive = s.IsActive,
        ActivityData = s.ActivityData.Select(a => a.ToReadingActivityDataDto()).ToList(),
        UserId = s.AppUserId,
        Username = s.AppUser.UserName!,
    };
}
