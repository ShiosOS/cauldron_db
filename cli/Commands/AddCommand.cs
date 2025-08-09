using System.ComponentModel;
using Cauldron.Cli.Inventory;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cauldron.Cli.Commands;

public sealed class AddCommand(IInventory inv) : AsyncCommand<AddCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<name>")] public string Name { get; set; } = "";
        [Description("YYYY-MM-DD")] [CommandOption("--expires")] public string? Expires { get; set; }
    }
    
    private readonly IInventory _inv = inv;

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        DateOnly? expires = null;
        if (!string.IsNullOrWhiteSpace(settings.Expires))
        {
            if (!DateOnly.TryParse(settings.Expires, out var d))
                throw new FormatException("Invalid expires format. Use YYYY-MM-DD");
            if (d < DateOnly.FromDateTime(DateTime.Today))
                throw new FormatException("Expiry can't be in the past.");
            expires = d;
        }

        var item = await _inv.AddAsync(settings.Name, expires);
        AnsiConsole.Markup($"[green]Added[/]: {item.Name} ([grey]{item.Id}[/])"
                           + (item.Expires is { } e ? $" expires {e}" : ""));
        return 0;
    }
}