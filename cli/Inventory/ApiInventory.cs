using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cauldron.Cli.Models;
using Cauldron.Cli.Security;

namespace Cauldron.Cli.Inventory;

public sealed partial class ApiInventory(string apiBase) : IInventory
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(apiBase) };
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static string? ExtractAccessToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();

        if (raw.Count(c => c == '.') == 2) return raw;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("access_token", out var at))
                return at.GetString();
        }
        catch { /* not JSON */ }

        var m = MyRegex().Match(raw);
        return m.Success ? m.Groups[1].Value : raw.Replace("\"", "").Replace("\r", "").Replace("\n", "").Trim();
    }

    private bool TryAttachAuth()
    {
        var stored = TokenStore.Load("auth-token");
        var token = ExtractAccessToken(stored ?? "");
        if (string.IsNullOrWhiteSpace(token)) return false;

        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return true;
    }


    public async Task<FoodItem> AddAsync(string name, DateOnly? expires, CancellationToken ct = default)
    {
        if (!TryAttachAuth())
            throw new InvalidOperationException("Not authenticated. Run: cauldron login");

        var body = new
        {
            name,
            expires = expires?.ToString("yyyy-MM-dd")
        };

        var resp = await _http.PostAsync("/items",
            new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json"), ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Unauthorized. Run: cauldron login.");

        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<FoodItem>(_json, ct) ?? new FoodItem(Guid.Empty, name, expires, DateTime.UtcNow);

        return created;
    }

    public async Task<IReadOnlyList<FoodItem>> ListAsync(CancellationToken ct = default)
    {
        if (!TryAttachAuth())
            throw new InvalidOperationException("Not authenticated. Run: cauldron login");

        var resp = await _http.GetAsync("/items", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Unauthorized. Run: cauldron login.");

        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<FoodItem>>(_json, ct);
        return items ?? new List<FoodItem>();
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken ct = default)
    {
        if (!TryAttachAuth())
            throw new InvalidOperationException("Not authenticated. Run: cauldron login");

        var resp = await _http.DeleteAsync($"/items/{id}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Unauthorized. Run: cauldron login.");

        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        resp.EnsureSuccessStatusCode();
        return true;
    }

    [GeneratedRegex(@"""access_token""\s*:\s*""([^""]+)""")]
    private static partial Regex MyRegex();
}
