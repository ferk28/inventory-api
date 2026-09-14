using Microsoft.AspNetCore.Cors.Infrastructure;
namespace Inventory.Api.Cors;
public static class CorsExtensions
{
    public const string PolicyName = "InventoryBrowserClients";
    public static IServiceCollection AddInventoryCors(this IServiceCollection services, IConfiguration configuration)
    {
        CorsSettings settings = CorsSettingsReader.FromEnvironment(configuration);
        services.AddSingleton(settings);
        services.AddCors(options => options.AddPolicy(PolicyName, policy => AllowBrowserClients(policy, settings)));

        return services;
    }
    private static void AllowBrowserClients(CorsPolicyBuilder policy, CorsSettings settings)
    {
        policy.WithOrigins(settings.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location");
    }
}
