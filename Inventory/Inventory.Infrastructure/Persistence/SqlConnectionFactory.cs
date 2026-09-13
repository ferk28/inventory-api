using System.Data;
using Inventory.Application.Abstractions.Persistence;
using Microsoft.Data.SqlClient;
namespace Inventory.Infrastructure.Persistence;
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }
    public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        SqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return connection;
    }
}
