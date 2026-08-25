using Microsoft.AspNetCore.Identity;

namespace Librariann.Models.Entities.User;

public class AppUserRole : IdentityUserRole<int>
{
    public AppUser User { get; set; } = null!;
    public AppRole Role { get; set; } = null!;
}
