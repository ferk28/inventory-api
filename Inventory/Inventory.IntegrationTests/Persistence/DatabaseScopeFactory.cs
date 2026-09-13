using Inventory.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Inventory.IntegrationTests.Persistence;
public static class DatabaseScopeFactory
{
    public static ServiceProvider CreateProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(DefaultSettings())
            .AddEnvironmentVariables()
            .Build();
        ServiceCollection services = new();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
    private static Dictionary<string, string?> DefaultSettings()
    {
        return new Dictionary<string, string?>
        {
            ["DB_HOST"] = "localhost",
            ["DB_PORT"] = "1433",
            ["DB_NAME"] = "InventoryDb",
            ["DB_USER"] = "sa",
            ["DB_PASSWORD"] = "admin"
        };
    }
}
