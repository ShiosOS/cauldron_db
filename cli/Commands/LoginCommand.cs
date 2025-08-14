using System.Text;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Cauldron.Cli.Security;

namespace Cauldron.Cli.Commands;

public sealed class LoginCommand : AsyncCommand<LoginCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--email")]   [Description("Email address")]
        public string? Email { get; init; }

        [CommandOption("--password")] [Description("Password (omit to be prompted)")]
        public string? Password { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var apiBase = Environment.GetEnvironmentVariable("API_BASE") ?? "http://localhost:5180";

        var email = settings.Email ?? AnsiConsole.Ask<string>("Email:");
        var password = settings.Password ?? AnsiConsole.Prompt(
            new TextPrompt<string>("Password:").Secret());

        using var http = new HttpClient();
        http.BaseAddress = new Uri(apiBase);
        using var content = new StringContent(
            JsonSerializer.Serialize(new { email, password }),
            Encoding.UTF8, "application/json");

        try
        {
            var resp = await http.PostAsync("/auth/login", content);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                AnsiConsole.MarkupLine($"[red]Login failed ({(int)resp.StatusCode}).[/] {body}");
                return 1;
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var token = doc.RootElement.GetProperty("access_token").GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                AnsiConsole.MarkupLine("[red]Login response did not include access_token[/]");
                return 1;
            }

            TokenStore.Save("auth-token", token!); 
            AnsiConsole.MarkupLine("[green]Login successful.[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error contacting server:[/] {ex.Message}");
            return 1;
        }
    }
}
