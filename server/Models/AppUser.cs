using Microsoft.AspNetCore.Identity;

namespace Cauldron.Server.Models;

public class AppUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public ICollection<UserHousehold> Memberships { get; set; } = new List<UserHousehold>();
}