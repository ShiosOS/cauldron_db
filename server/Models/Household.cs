namespace Cauldron.Server.Models;

public class Household
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? JoinCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserHousehold> Members { get; set; } = new List<UserHousehold>();
}