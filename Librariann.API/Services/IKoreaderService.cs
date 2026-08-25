using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Koreader;

namespace Librariann.API.Services;

public interface IKoreaderService
{
    Task SaveProgress(KoreaderBookDto koreaderBookDto, int userId, CancellationToken ct = default);
    Task<KoreaderBookDto> GetProgress(string bookHash, int userId, CancellationToken ct = default);
}
