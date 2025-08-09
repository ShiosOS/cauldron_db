using Cauldron.Cli.Inventory;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cauldron.Cli.Commands;

public sealed class PruneCommand(IInventory inv) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext ctx)
    {
        var items = await inv.ListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var expired = items.Where(i => i.Expires is { } e && e < today).Select(i => i.Id).ToList();
        var removed = 0;
        foreach (var id in expired)
            if (await inv.RemoveAsync(id)) removed++;

        AnsiConsole.MarkupLine(removed > 0
            ? $"[red]Pruned[/] {removed} expired item(s)."
            : "[green]No expired items.[/]");
        return 0;
    }
}
