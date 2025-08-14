namespace Cauldron.Server.Models;

public class UserHousehold
{
    public Guid UserId { get; set; }          // Guid to match AppUser.Id
    public Guid HouseholdId { get; set; }
    public string Role { get; set; } = "admin";

    public AppUser User { get; set; } = default!;
    public Household Household { get; set; } = default!;
}