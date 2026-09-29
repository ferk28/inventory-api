using Inventory.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Inventory.IntegrationTests.Persistence;
// No fallback values: the connection comes from the repository's .env (the same file
// docker compose reads) and environment variables override it. Anything missing makes
// DatabaseSettingsReader fail naming the variable, exactly as the API does at startup.
public static class DatabaseScopeFactory
{
    private const string DotEnvFileName = ".env";
    public static ServiceProvider CreateProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(ReadDotEnv())
            .AddEnvironmentVariables()
            .Build();
        ServiceCollection services = new();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
    private static Dictionary<string, string?> ReadDotEnv()
    {
        Dictionary<string, string?> values = new();
        string? path = FindDotEnv();
        if (path is null)
        {
            return values;
        }
        foreach (string line in File.ReadLines(path))
        {
            string trimmed = line.Trim();
            int separator = trimmed.IndexOf('=');
            if (trimmed.StartsWith('#') || separator <= 0)
            {
                continue;
            }
            values[trimmed[..separator].Trim()] = trimmed[(separator + 1)..].Trim();
        }

        return values;
    }
    private static string? FindDotEnv()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, DotEnvFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
