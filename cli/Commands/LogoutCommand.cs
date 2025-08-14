using Spectre.Console;
using Spectre.Console.Cli;
using Cauldron.Cli.Security;

namespace Cauldron.Cli.Commands;

public sealed class LogoutCommand : Command
{
    public override int Execute(CommandContext context)
    {
        TokenStore.Clear("auth-token");
        AnsiConsole.MarkupLine("[yellow]Logged out.[/]");
        return 0;
    }
}