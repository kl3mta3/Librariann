using System.Collections.Generic;
using System.Linq;
using Librariann.Models.DTOs.SeriesDetail;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Models.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<RecentlyAddedSeriesDto> RestrictAgainstAgeRestriction(this IEnumerable<RecentlyAddedSeriesDto> items, AgeRestriction restriction)
    {
        if (restriction.AgeRating == AgeRating.NotApplicable) return items;
        var q = items.Where(s => s.AgeRating <= restriction.AgeRating);
        if (!restriction.IncludeUnknowns)
        {
            return q.Where(s => s.AgeRating != AgeRating.Unknown);
        }

        return q;
    }

    public static IEnumerable<SeriesMetadata> RestrictAgainstAgeRestriction(this IEnumerable<SeriesMetadata> items, AgeRestriction restriction)
    {
        if (restriction.AgeRating == AgeRating.NotApplicable) return items;
        var q = items.Where(s => s.AgeRating <= restriction.AgeRating);
        if (!restriction.IncludeUnknowns)
        {
            return q.Where(s => s.AgeRating != AgeRating.Unknown);
        }

        return q;
    }

    public static IEnumerable<Chapter> RestrictAgainstAgeRestriction(this IEnumerable<Chapter> items, AgeRestriction restriction)
    {
        if (restriction.AgeRating == AgeRating.NotApplicable) return items;
        var q = items.Where(s => s.AgeRating <= restriction.AgeRating);
        if (!restriction.IncludeUnknowns)
        {
            return q.Where(s => s.AgeRating != AgeRating.Unknown);
        }

        return q;
    }
}
