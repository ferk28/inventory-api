using System.Data.Common;
using Inventory.Application.Abstractions.Persistence;
namespace Inventory.Infrastructure.Persistence;
public sealed class SqlConnectionContext : IAsyncDisposable
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private DbConnection? _connection;
    public SqlConnectionContext(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    public DbTransaction? CurrentTransaction { get; private set; }
    public async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        _connection ??= await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return _connection;
    }
    public async Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = await GetConnectionAsync(cancellationToken);
        CurrentTransaction = await connection.BeginTransactionAsync(cancellationToken);

        return CurrentTransaction;
    }
    public void ClearTransaction()
    {
        CurrentTransaction = null;
    }
    public async ValueTask DisposeAsync()
    {
        if (CurrentTransaction is not null)
        {
            await CurrentTransaction.DisposeAsync();
        }
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
