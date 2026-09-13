using System.Data.Common;
using Dapper;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Domain.Entities;
namespace Inventory.Infrastructure.Persistence.Repositories;
public sealed class CategoryWriteRepository : ICategoryWriteRepository
{
    private readonly SqlConnectionContext _connectionContext;
    public CategoryWriteRepository(SqlConnectionContext connectionContext)
    {
        _connectionContext = connectionContext;
    }
    public async Task<Category?> FindByIdAsync(int categoryId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, Name, Description, IsActive, CreatedAt
            FROM dbo.Categories
            WHERE Id = @CategoryId;
            """;
        DbConnection connection = await _connectionContext.GetConnectionAsync(cancellationToken);
        CommandDefinition command = new(sql, new { CategoryId = categoryId }, _connectionContext.CurrentTransaction, cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Category>(command);
    }
    public async Task<bool> HasActiveProductsAsync(int categoryId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Products WHERE CategoryId = @CategoryId AND IsActive = 1
            ) THEN 1 ELSE 0 END;
            """;
        DbConnection connection = await _connectionContext.GetConnectionAsync(cancellationToken);
        CommandDefinition command = new(sql, new { CategoryId = categoryId }, _connectionContext.CurrentTransaction, cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<bool>(command);
    }
    public async Task UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Categories
            SET Name = @Name, Description = @Description, IsActive = @IsActive
            WHERE Id = @Id;
            """;
        DbConnection connection = await _connectionContext.GetConnectionAsync(cancellationToken);
        CommandDefinition command = new(sql, category, _connectionContext.CurrentTransaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
