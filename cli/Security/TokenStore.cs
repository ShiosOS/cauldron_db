using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Cauldron.Cli.Security;

internal static class TokenStore
{
    private static readonly string BaseDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cauldron");

    private static string PathFor(string name)
    {
        Directory.CreateDirectory(BaseDir);
        return Path.Combine(BaseDir, $"{name}.bin");
    }

    private static readonly IDataProtector Protector = CreateProtector();

    private static IDataProtector CreateProtector()
    {
        Directory.CreateDirectory(BaseDir);
        var keysDir = Path.Combine(BaseDir, "keys");
        Directory.CreateDirectory(keysDir);

        var services = new ServiceCollection();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
            .SetApplicationName("Cauldron.Cli");

        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IDataProtectionProvider>();
        return provider.CreateProtector("Cauldron.Cli.TokenStore.v1");
    }

    public static void Save(string name, string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var protectedBytes = Protector.Protect(bytes);
        File.WriteAllBytes(PathFor(name), protectedBytes);
    }

    public static string? Load(string name)
    {
        var file = PathFor(name);
        if (!File.Exists(file)) return null;

        var protectedBytes = File.ReadAllBytes(file);
        var bytes = Protector.Unprotect(protectedBytes);
        return Encoding.UTF8.GetString(bytes);
    }

    public static void Clear(string name)
    {
        var file = PathFor(name);
        if (File.Exists(file)) File.Delete(file);
    }
}