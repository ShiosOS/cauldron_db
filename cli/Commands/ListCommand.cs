using System.Runtime.CompilerServices;
using Cauldron.Cli.Inventory;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cauldron.Cli.Commands;

public class ListCommand(IInventory inv) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var items = await inv.ListAsync();
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items found.[/]");
            return 0;
        }
        
        items = items
            .OrderBy(it => it.Expires ?? DateOnly.MaxValue)
            .ToList();
        
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Id");
        table.AddColumn("Name");
        table.AddColumn("Expires");
        table.AddColumn("In");
        
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var item in items)
        {
            var expires = item.Expires?.ToString() ?? "-";

            var inStr = "-";
            var style = new Style();

            if (item.Expires is { } e)
            {
                var days = e.DayNumber - today.DayNumber;
                inStr = days == 0 ? "today" : $"{days}d";
                style = days < 0 ? new Style(foreground: Color.Red)
                    : days <= 2 ? new Style(foreground: Color.Yellow)
                    : new Style(foreground: Color.Green);
            }

            table.AddRow(
                new Text(item.Id.ToString()),
                new Text(item.Name),          
                new Text(expires),
                new Text(inStr, style)     
            );
        }

        AnsiConsole.Write(table);
        return 0;
    }
}