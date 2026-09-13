using System.Data.Common;
namespace Inventory.Application.Abstractions.Persistence;
public interface ISqlConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
