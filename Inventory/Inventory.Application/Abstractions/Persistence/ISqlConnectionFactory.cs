using System.Data;
namespace Inventory.Application.Abstractions.Persistence;
public interface ISqlConnectionFactory
{
    Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
