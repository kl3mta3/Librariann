using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Models.Mapping;
using Librariann.Models.DTOs.ReadingLists.CBL.RemapRules;
using Librariann.Models.Entities;
using Librariann.Models.Entities.ReadingLists;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public class ReadingListRemapRuleRepository(DataContext context) : IReadingListRemapRuleRepository
{
    public async Task<IList<ReadingListRemapRule>> GetRulesForNamesAsync(IList<string> normalizedNames, int userId, CancellationToken ct = default)
    {
        return await context.ReadingListRemapRule
            .Where(r => normalizedNames.Contains(r.NormalizedCblSeriesName)
                        && (r.AppUserId == userId || r.IsGlobal))
            .OrderByDescending(r => r.AppUserId == userId) // user-specific first
            .ToListAsync(ct);
    }

    public async Task<IList<RemapRuleDto>> GetRuleDtosForUserAsync(int userId, CancellationToken ct = default)
    {
        return await context.ReadingListRemapRule
            .Include(r => r.AppUser)
            .Include(r => r.Chapter)
            .Include(r => r.Volume)
            .Include(r => r.Series).ThenInclude(s => s.Library)
            .Where(r => r.AppUserId == userId || r.IsGlobal)
            .OrderByDescending(r => r.AppUserId == userId)
            .ThenByDescending(r => r.CreatedUtc)
            .Select(ReadingListRemapRuleMapping.ToRemapRuleDtoExpression)
            .ToListAsync(ct);
    }

    public async Task<ReadingListRemapRule?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.ReadingListRemapRule
            .Include(r => r.AppUser)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<RemapRuleDto?> GetDtoByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.ReadingListRemapRule
            .Include(r => r.AppUser)
            .Where(r => r.Id == id)
            .Select(ReadingListRemapRuleMapping.ToRemapRuleDtoExpression)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IList<RemapRuleDto>> GetAllRuleDtosAsync(CancellationToken ct = default)
    {
        return await context.ReadingListRemapRule
            .Include(r => r.AppUser)
            .OrderByDescending(r => r.IsGlobal)
            .ThenBy(r => r.NormalizedCblSeriesName)
            .Select(ReadingListRemapRuleMapping.ToRemapRuleDtoExpression)
            .ToListAsync(ct);
    }

    public async Task<ReadingListRemapRule?> GetExactRuleAsync(string normalizedCblSeriesName, string? cblVolume, string? cblNumber, int userId, CancellationToken ct = default)
    {
        return await context.ReadingListRemapRule
            .FirstOrDefaultAsync(r =>
                r.NormalizedCblSeriesName == normalizedCblSeriesName
                && r.CblVolume == cblVolume
                && r.CblNumber == cblNumber
                && r.AppUserId == userId, ct);
    }

    public void Add(ReadingListRemapRule rule)
    {
        context.ReadingListRemapRule.Add(rule);
    }

    public void Remove(ReadingListRemapRule rule)
    {
        context.ReadingListRemapRule.Remove(rule);
    }
}
