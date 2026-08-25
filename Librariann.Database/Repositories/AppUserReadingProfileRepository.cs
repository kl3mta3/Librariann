using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Mapping;
using Librariann.API.Repositories;
using Librariann.Common.Extensions;
using Librariann.Database.Extensions;
using Librariann.Models.DTOs;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;


public class AppUserReadingProfileRepository(DataContext context): IAppUserReadingProfileRepository
{

    public Task<AppUserReadingProfile> GetProfileForSeries(int userId, int libraryId, int seriesId,
        int? activeDeviceId = null, bool skipImplicit = false, CancellationToken ct = default)
    {
        return context.AppUserReadingProfiles
            .Where(rp => rp.AppUserId == userId)
            .WhereIf(skipImplicit, rp => rp.Kind != ReadingProfileKind.Implicit)
            .Where(rp => rp.DeviceIds.Count == 0 || activeDeviceId == null || rp.DeviceIds.Contains(activeDeviceId.Value))
            .OrderByDescending(rp => rp.Kind == ReadingProfileKind.Implicit && rp.SeriesIds.Contains(seriesId) && (rp.DeviceIds.Count == 0 || (activeDeviceId != null && rp.DeviceIds.Contains(activeDeviceId.Value))))
            .ThenByDescending(rp => rp.Kind == ReadingProfileKind.Implicit && rp.SeriesIds.Contains(seriesId))
            .ThenByDescending(rp => rp.SeriesIds.Contains(seriesId) && (rp.DeviceIds.Count == 0 || (activeDeviceId != null && rp.DeviceIds.Contains(activeDeviceId.Value))))
            .ThenByDescending(rp => rp.SeriesIds.Contains(seriesId))
            .ThenByDescending(rp => rp.LibraryIds.Contains(libraryId) && (rp.DeviceIds.Count == 0 || (activeDeviceId != null && rp.DeviceIds.Contains(activeDeviceId.Value))))
            .ThenByDescending(rp => rp.LibraryIds.Contains(libraryId))
            .ThenByDescending(rp => rp.Kind == ReadingProfileKind.Default)
            .FirstAsync(ct);
    }

    public Task<List<AppUserReadingProfile>> GetProfilesForLibrary(int userId, int libraryId,
        CancellationToken ct = default)
    {
        return context.AppUserReadingProfiles
            .Where(rp => rp.AppUserId == userId && rp.LibraryIds.Contains(libraryId))
            .ToListAsync(ct);
    }

    public async Task<IList<AppUserReadingProfile>> GetProfilesByFontFamily(string family, CancellationToken ct = default)
    {
        return await context.AppUserReadingProfiles
            .Where(rp => rp.BookReaderFontFamily == family)
            .ToListAsync(ct);
    }

    public async Task<AppUserReadingProfile?> GetUserProfile(int userId, int profileId, CancellationToken ct = default)
    {
        return await context.AppUserReadingProfiles
            .Where(rp => rp.AppUserId == userId && rp.Id == profileId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IList<AppUserReadingProfile>> GetProfilesForUser(int userId, bool skipImplicit = false,
        CancellationToken ct = default)
    {
        return await context.AppUserReadingProfiles
            .Where(rp => rp.AppUserId == userId)
            .WhereIf(skipImplicit, rp => rp.Kind != ReadingProfileKind.Implicit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns all Reading Profiles for the User
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="skipImplicit"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IList<UserReadingProfileDto>> GetProfilesDtoForUser(int userId, bool skipImplicit = false,
        CancellationToken ct = default)
    {
        return await context.AppUserReadingProfiles
            .Where(rp => rp.AppUserId == userId)
            .WhereIf(skipImplicit, rp => rp.Kind != ReadingProfileKind.Implicit)
            .Select(ReadingProfileMapping.ToUserReadingProfileDtoExpression)
            .ToListAsync(ct);
    }

    public async Task<bool> IsProfileNameInUse(int userId, string name, CancellationToken ct = default)
    {
        var normalizedName = name.ToNormalized();

        return await context.AppUserReadingProfiles
            .Where(rp => rp.NormalizedName == normalizedName && rp.AppUserId == userId)
            .AnyAsync(ct);
    }

    public void Add(AppUserReadingProfile readingProfile)
    {
        context.AppUserReadingProfiles.Add(readingProfile);
    }

    public void Update(AppUserReadingProfile readingProfile)
    {
        context.AppUserReadingProfiles.Update(readingProfile).State = EntityState.Modified;
    }

    public void Remove(AppUserReadingProfile readingProfile)
    {
        context.AppUserReadingProfiles.Remove(readingProfile);
    }

    public void RemoveRange(IEnumerable<AppUserReadingProfile> readingProfiles)
    {
        context.AppUserReadingProfiles.RemoveRange(readingProfiles);
    }
}
