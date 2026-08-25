using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Mapping;
using Librariann.API.Repositories;
using Librariann.Common.Extensions;
using Librariann.Models;
using Librariann.Models.DTOs.Font;
using Librariann.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public class EpubFontRepository(DataContext context) : IEpubFontRepository
{
    public void Add(EpubFont font)
    {
        context.Add(font);
    }

    public void Remove(EpubFont font)
    {
        context.Remove(font);
    }

    public void Update(EpubFont font)
    {
        context.Entry(font).State = EntityState.Modified;
    }

    public async Task<IList<EpubFontDto>> GetFontDtosAsync(CancellationToken ct = default)
    {
        return await context.EpubFont
            .OrderBy(s => s.Name == Defaults.DefaultFont ? -1 : 0)
            .ThenBy(s => s)
            .Select(EpubFontMapping.ToEpubFontDtoExpression)
            .ToListAsync(ct);
    }

    public async Task<EpubFontDto?> GetFontDtoAsync(int fontId, CancellationToken ct = default)
    {
        return await context.EpubFont
            .Where(f => f.Id == fontId)
            .Select(EpubFontMapping.ToEpubFontDtoExpression)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EpubFontDto?> GetFontDtoByNameAsync(string name, CancellationToken ct = default)
    {
        return await context.EpubFont
            .Where(f => f.NormalizedName.Equals(name.ToNormalized()))
            .Select(EpubFontMapping.ToEpubFontDtoExpression)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IList<EpubFont>> GetFontsAsync(CancellationToken ct = default)
    {
        return await context.EpubFont.ToListAsync(ct);
    }

    public async Task<EpubFont?> GetFontAsync(int fontId, CancellationToken ct = default)
    {
        return await context.EpubFont
            .Where(f => f.Id == fontId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EpubFont?> GetFontByNameAsync(string name, CancellationToken ct = default)
    {
        return await context.EpubFont
            .Where(f => f.NormalizedName.Equals(name.ToNormalized()))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> IsFontFamilyInUseAsync(string family, CancellationToken ct = default)
    {
        return await context.AppUserReadingProfiles
            .AnyAsync(rp => rp.BookReaderFontFamily == family, ct);
    }

    public async Task<IList<EpubFont>> GetFontsByFamilyAsync(string family, CancellationToken ct = default)
    {
        return await context.EpubFont
            .Where(f => f.Family == family)
            .ToListAsync(ct);
    }

}
