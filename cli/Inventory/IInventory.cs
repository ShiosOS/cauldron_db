using Cauldron.Cli.Models;

namespace Cauldron.Cli.Inventory;

public interface IInventory
{
   Task<FoodItem> AddAsync(string name, DateOnly? expires, CancellationToken ct = default);
   Task<IReadOnlyList<FoodItem>> ListAsync(CancellationToken ct = default);
   Task<bool> RemoveAsync(Guid id, CancellationToken ct = default);
}