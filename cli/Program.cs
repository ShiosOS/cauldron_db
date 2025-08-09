using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Cauldron.Cli.Inventory;
using Cauldron.Cli.Commands;

Env.Load();

var store = Environment.GetEnvironmentVariable("CAULDRON_STORE") ?? "file";

if (store.Equals("pg", StringComparison.OrdinalIgnoreCase))
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
    var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
    var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "cauldron";

    var cs = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPass};Database={dbName}";
    Environment.SetEnvironmentVariable("DATABASE_URL", cs);

    Console.WriteLine("DATABASE_URL: " + cs.Replace(dbPass, "***"));

    try
    {
        await using var con = new Npgsql.NpgsqlConnection(cs);
        await con.OpenAsync();
        Console.WriteLine($"Connected as {dbUser} to {dbName}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Postgres connection failed: " + ex.Message);
        return 1;
    }
}

var services = new ServiceCollection();

if (store.Equals("pg", StringComparison.OrdinalIgnoreCase))
{
    var cs = Environment.GetEnvironmentVariable("DATABASE_URL")!;
    services.AddSingleton<IInventory>(_ => new PostgresInventory(cs));
}
else
{
    services.AddSingleton<IInventory, FileInventory>();
}

services
    .AddSingleton<AddCommand>()
    .AddSingleton<ListCommand>()
    .AddSingleton<RemoveCommand>()
    .AddSingleton<PruneCommand>()
    .AddSingleton<SoonCommand>();

var app = new CommandApp(new TypeRegistrar(services));
app.Configure(cfg =>
{
    cfg.SetApplicationName("cauldron");
    cfg.AddCommand<AddCommand>("add").WithDescription("Adds a new item to the inventory");
    cfg.AddCommand<ListCommand>("list").WithDescription("Lists all items");
    cfg.AddCommand<RemoveCommand>("remove").WithDescription("Removes an item from the inventory");
    cfg.AddCommand<PruneCommand>("prune").WithDescription("Delete all expired items");
    cfg.AddCommand<SoonCommand>("soon").WithDescription("List items expiring within N days");
});

return await app.RunAsync(args);

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _builder;
    public TypeRegistrar(IServiceCollection builder) => _builder = builder;
    public ITypeResolver Build() => new TypeResolver(_builder.BuildServiceProvider());
    public void Register(Type service, Type implementation) => _builder.AddSingleton(service, implementation);
    public void RegisterInstance(Type service, object implementation) => _builder.AddSingleton(service, implementation);
    public void RegisterLazy(Type service, Func<object> factory) => _builder.AddSingleton(service, _ => factory());
}

public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly ServiceProvider _provider;
    public TypeResolver(ServiceProvider provider) => _provider = provider;
    public object? Resolve(Type? type) => type is null ? null : _provider.GetService(type);
    public void Dispose() => _provider.Dispose();
}
