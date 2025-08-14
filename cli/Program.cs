using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Cauldron.Cli.Inventory;
using Cauldron.Cli.Commands;

Env.Load(); // loads .env if present (working dir)

var services = new ServiceCollection();

var store = Environment.GetEnvironmentVariable("CAULDRON_STORE") ?? "api";

if (store.Equals("api", StringComparison.OrdinalIgnoreCase))
{
    var apiBase = Environment.GetEnvironmentVariable("API_BASE") ?? "http://localhost:5180";
    services.AddSingleton<IInventory>(_ => new ApiInventory(apiBase));
}
else if (store.Equals("file", StringComparison.OrdinalIgnoreCase))
{
    services.AddSingleton<IInventory, FileInventory>();
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
    .AddSingleton<SoonCommand>()
    .AddSingleton<LoginCommand>()
    .AddSingleton<LogoutCommand>();

var app = new CommandApp(new TypeRegistrar(services));
app.Configure(cfg =>
{
    cfg.SetApplicationName("cauldron");
    cfg.AddCommand<AddCommand>("add").WithDescription("Adds a new item to the inventory");
    cfg.AddCommand<ListCommand>("list").WithDescription("Lists all items");
    cfg.AddCommand<RemoveCommand>("remove").WithDescription("Removes an item from the inventory");
    cfg.AddCommand<PruneCommand>("prune").WithDescription("Delete all expired items");
    cfg.AddCommand<SoonCommand>("soon").WithDescription("List items expiring within N days");
    cfg.AddCommand<LoginCommand>("login").WithDescription("Sign in and save a token");
    cfg.AddCommand<LogoutCommand>("logout").WithDescription("Clear saved token");
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
