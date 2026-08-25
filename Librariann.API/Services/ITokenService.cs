using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Account;
using Librariann.Models.Entities.User;

namespace Librariann.API.Services;

public interface ITokenService
{
    Task<string> CreateToken(AppUser user, CancellationToken ct = default);
    Task<TokenRequestDto?> ValidateRefreshToken(TokenRequestDto request, CancellationToken ct = default);
    Task<string> CreateRefreshToken(AppUser user, CancellationToken ct = default);
    Task<string?> GetJwtFromUser(AppUser user, CancellationToken ct = default);
}
