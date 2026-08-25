using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Librariann.Models.Entities.User;

public class AppRole : IdentityRole<int>
{
    public ICollection<AppUserRole> UserRoles { get; set; } = null!;
}
