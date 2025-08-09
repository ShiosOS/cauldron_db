using System.Text.Json;
using Cauldron.Cli.Models;

namespace Cauldron.Cli.Inventory;

public sealed class FileInventory : IInventory
{
   private readonly string _path;
   private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
   private readonly SemaphoreSlim _gate = new(1, 1);

   public FileInventory(string? path = null)
   {
      var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cauldron");
      Directory.CreateDirectory(dir);
      _path = path ?? Path.Combine(dir, "inventory.json");
   }
   
   public async Task<FoodItem> AddAsync(string name, DateOnly? expires, CancellationToken ct = default)
   {
      var items = await LoadAsync(ct);
      var item = new FoodItem(Guid.NewGuid(), name, expires, DateTime.UtcNow);
      items.Add(item);
      await SaveAsync(items, ct);
      return item;
   }

   public async Task<IReadOnlyList<FoodItem>> ListAsync(CancellationToken ct = default) => await LoadAsync(ct);

   public async Task<bool> RemoveAsync(Guid id, CancellationToken ct = default)
   {
      var items = await LoadAsync(ct);
      var removed = items.RemoveAll(i => i.Id == id) > 0;
      if (removed)
         await SaveAsync(items, ct);
      return removed;
   }

   private async Task<List<FoodItem>> LoadAsync(CancellationToken ct)
   {
      await _gate.WaitAsync(ct);
      try
      {
         if (!File.Exists(_path))
            return [];

         await using var s = File.OpenRead(_path);
         var items = await JsonSerializer.DeserializeAsync<List<FoodItem>>(s, _json, ct);
         return items ?? [];
      }
      finally
      {
         _gate.Release();
      }
   }

   private async Task SaveAsync(List<FoodItem> items, CancellationToken ct)
   {
      await _gate.WaitAsync(ct);
      if (File.Exists(_path))
      {
         var bak = _path + ".bak";
         File.Copy(_path, bak, overwrite: true);
      }
      try
      {
         
         await using var s = File.Create(_path);
         await JsonSerializer.SerializeAsync(s, items, _json, ct);
      }
      finally
      {
         _gate.Release();
      }
   }
}