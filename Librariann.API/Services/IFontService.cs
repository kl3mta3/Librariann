using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Font;
using Librariann.Models.Entities;

namespace Librariann.API.Services;

public interface IFontService
{
    Task<EpubFont> CreateFontFromFileAsync(string path, CancellationToken ct = default);
    Task<FontDeleteResultDto> DeleteFamily(int fontId, bool forceDelete, CancellationToken ct = default);
    Task<EpubFont[]> CreateFontsFromUrl(string url, CancellationToken ct = default);
}
