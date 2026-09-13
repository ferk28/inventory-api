using Microsoft.Data.SqlClient;
namespace Inventory.Infrastructure.Persistence;
public static class ConnectionStringBuilder
{
    public static string Build(DatabaseSettings settings)
    {
        SqlConnectionStringBuilder builder = new()
        {
            DataSource = $"{settings.Host},{settings.Port}",
            InitialCatalog = settings.Name,
            UserID = settings.User,
            Password = settings.Password,
            TrustServerCertificate = true,
            Encrypt = true
        };

        return builder.ConnectionString;
    }
}
