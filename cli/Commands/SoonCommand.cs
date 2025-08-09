using Cauldron.Cli.Inventory;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cauldron.Cli.Commands;

public sealed class SoonCommand(IInventory inv) : AsyncCommand<SoonCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--days")] public int Days { get; init; } = 3;
    }

    public override async Task<int> ExecuteAsync(CommandContext ctx, Settings s)
    {
        var items = await inv.ListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var soon = items
            .Where(it => it.Expires is { } e
                         && e >= today
                         && e.DayNumber - today.DayNumber <= s.Days)
            .OrderBy(it => it.Expires).ToList();

        if (soon.Count == 0) { AnsiConsole.MarkupLine("[yellow]Nothing expiring soon.[/]"); return 0; }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Name"); table.AddColumn("Expires"); table.AddColumn("In");
        foreach (var it in soon)
        {
            var days = it!.Expires!.Value.DayNumber - today.DayNumber;
            var style = days == 0 ? new Style(foreground: Color.Yellow) : new Style(foreground: Color.Green);
            table.AddRow(new Text(it.Name), new Text(it.Expires!.ToString()!), new Text(days == 0 ? "today" : $"{days}d", style));
        }
        AnsiConsole.Write(table);
        return 0;
    }
}