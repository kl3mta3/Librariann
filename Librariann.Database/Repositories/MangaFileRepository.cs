using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public class MangaFileRepository(DataContext context) : IMangaFileRepository
{
    public void Update(MangaFile file)
    {
        context.Entry(file).State = EntityState.Modified;
    }

    public async Task<IList<MangaFile>> GetAllWithMissingExtension(CancellationToken ct = default)
    {
        return await context.MangaFile
            .Where(f => string.IsNullOrEmpty(f.Extension))
            .ToListAsync(ct);
    }

    public async Task<MangaFile?> GetByKoreaderHash(string hash, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(hash)) return null;

        return await context.MangaFile
            .FirstOrDefaultAsync(f => f.KoreaderHash != null &&
                                    f.KoreaderHash.Equals(hash.ToUpper()), ct);
    }
}
