using Inventory.Application.Abstractions.Persistence;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Inventory.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DatabaseSettings settings = DatabaseSettingsReader.FromEnvironment(configuration);
        string connectionString = ConnectionStringBuilder.Build(settings);
        services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IInventoryReadContext>(provider => provider.GetRequiredService<InventoryDbContext>());
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<SqlConnectionContext>();
        services.AddScoped<IUnitOfWork, SqlUnitOfWork>();
        services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
        services.AddScoped<ICategoryWriteRepository, CategoryWriteRepository>();
        services.AddScoped<IInventoryMovementWriteRepository, InventoryMovementWriteRepository>();

        return services;
    }
}
