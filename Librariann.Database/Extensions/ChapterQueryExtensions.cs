using System.Linq;
using Librariann.Common.Constants;
using Librariann.Models.Constants;
using Librariann.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Extensions;

public static class ChapterQueryExtensions
{
    public static IOrderedQueryable<Chapter> ApplyDefaultChapterOrdering(this IQueryable<Chapter> query)
    {
        return query
            .Include(c => c.Volume)
            .OrderBy(c =>
                // Priority 1: Regular volumes (not loose-leaf, not special)
                c.Volume.MinNumber == ParserConstants.LooseLeafVolumeNumber ||
                c.Volume.MinNumber == ParserConstants.SpecialVolumeNumber ? 1 : 0)
            .ThenBy(c =>
                // Priority 2: Loose leaf over specials
                c.Volume.MinNumber == ParserConstants.SpecialVolumeNumber ? 1 : 0)
            // Priority 3: Non-special chapters
            .ThenBy(c => c.IsSpecial ? 1 : 0)
            .ThenBy(c => c.Volume.MinNumber)
            .ThenBy(c => c.SortOrder);
    }
}
