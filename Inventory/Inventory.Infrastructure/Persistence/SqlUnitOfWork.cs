using System.Data.Common;
using Inventory.Application.Abstractions.Persistence;
namespace Inventory.Infrastructure.Persistence;
public sealed class SqlUnitOfWork : IUnitOfWork
{
    private readonly SqlConnectionContext _connectionContext;
    public SqlUnitOfWork(SqlConnectionContext connectionContext)
    {
        _connectionContext = connectionContext;
    }
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        DbTransaction transaction = await _connectionContext.BeginTransactionAsync(cancellationToken);
        try
        {
            TResult result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _connectionContext.ClearTransaction();
            await transaction.DisposeAsync();
        }
    }
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        return ExecuteInTransactionAsync(token => RunAndSignalCompletionAsync(operation, token), cancellationToken);
    }
    private static async Task<bool> RunAndSignalCompletionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await operation(cancellationToken);

        return true;
    }
}
