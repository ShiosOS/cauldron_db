using Cauldron.Cli.Inventory;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cauldron.Cli.Commands;

public sealed class RemoveCommand(IInventory inv) : AsyncCommand<RemoveCommand.Settings>
{
   public sealed class Settings : CommandSettings
   {
     [CommandArgument(0, "<id>")] public Guid Id { get; init; } 
   }

   public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
   {
       var ok = await inv.RemoveAsync(settings.Id);
       AnsiConsole.MarkupLine(ok 
           ? "[red]Removed[/]"
           : "[yellow]No items found.[/]");
       return ok ? 0 : 1;
   }
}