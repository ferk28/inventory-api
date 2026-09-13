using Microsoft.Extensions.Configuration;
namespace Inventory.Infrastructure.Persistence;
public static class DatabaseSettingsReader
{
    public static DatabaseSettings FromEnvironment(IConfiguration configuration)
    {
        return new DatabaseSettings
        {
            Host = RequireValue(configuration, "DB_HOST"),
            Port = int.Parse(RequireValue(configuration, "DB_PORT")),
            Name = RequireValue(configuration, "DB_NAME"),
            User = RequireValue(configuration, "DB_USER"),
            Password = RequireValue(configuration, "DB_PASSWORD")
        };
    }
    private static string RequireValue(IConfiguration configuration, string key)
    {
        string? value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is required but was not set.");
        }

        return value;
    }
}
