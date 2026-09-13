using System.Data.Common;
using Dapper;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Domain.Entities;
namespace Inventory.Infrastructure.Persistence.Repositories;
public sealed class InventoryMovementWriteRepository : IInventoryMovementWriteRepository
{
    private readonly SqlConnectionContext _connectionContext;
    public InventoryMovementWriteRepository(SqlConnectionContext connectionContext)
    {
        _connectionContext = connectionContext;
    }
    public async Task<int> AddAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.InventoryMovements (ProductId, Type, Quantity, Reason, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@ProductId, @Type, @Quantity, @Reason, @CreatedAt);
            """;
        DbConnection connection = await _connectionContext.GetConnectionAsync(cancellationToken);
        CommandDefinition command = new(sql, movement, _connectionContext.CurrentTransaction, cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<int>(command);
    }
}
