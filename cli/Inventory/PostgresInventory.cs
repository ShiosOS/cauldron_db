using Npgsql;
using Cauldron.Cli.Models;

namespace Cauldron.Cli.Inventory;

public sealed class PostgresInventory(string connectionString) : IInventory
{
    public async Task<FoodItem> AddAsync(string name, DateOnly? expires, CancellationToken ct = default)
    {
        var item = new FoodItem(Guid.NewGuid(), name, expires, DateTime.UtcNow);

        await using var con = new NpgsqlConnection(connectionString);
        await con.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand("""

                                                            insert into food_items (id, name, expires, created_at)
                                                            values (@id, @name, @expires, @created_at);
                                                        
                                                """, con);

        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.Parameters.AddWithValue("name", item.Name);
        cmd.Parameters.AddWithValue("created_at", item.CreatedAt);

        if (item.Expires is { } d)
            cmd.Parameters.AddWithValue("expires", NpgsqlTypes.NpgsqlDbType.Date, d.ToDateTime(new TimeOnly()));
        else
            cmd.Parameters.AddWithValue("expires", NpgsqlTypes.NpgsqlDbType.Date, DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
        return item;
    }

    public async Task<IReadOnlyList<FoodItem>> ListAsync(CancellationToken ct = default)
    {
        var list = new List<FoodItem>();

        await using var con = new NpgsqlConnection(connectionString);
        await con.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand("""
                                                                            select id, name, expires, created_at
                                                                            from food_items
                                                                            order by coalesce(expires, (now() + interval '100 years')) , created_at;
                                                                """, con);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var id = r.GetGuid(0);
            var name = r.GetString(1);
            DateOnly? expires = r.IsDBNull(2) ? null : DateOnly.FromDateTime(r.GetDateTime(2));
            var created = r.GetDateTime(3).ToUniversalTime();

            list.Add(new FoodItem(id, name, expires, created));
        }
        return list;
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = new NpgsqlConnection(connectionString);
        await con.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand("delete from food_items where id = @id;", con);
        cmd.Parameters.AddWithValue("id", id);
        var n = await cmd.ExecuteNonQueryAsync(ct);
        return n > 0;
    }
}
