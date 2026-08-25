using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.LibrariannPlus.OAuth;
using Librariann.Models.Entities.User;

namespace Librariann.API.Services.Plus;

public interface IOAuthService
{
    Task HandleCallback(AppUser user, OAuthUpstream upstream, string token, string? refreshToken = null);

    Task RefreshTokens(CancellationToken ct = default);
}
