using System.Data.Common;
using Dapper;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Domain.Entities;
namespace Inventory.Infrastructure.Persistence.Repositories;
public sealed class ProductWriteRepository : IProductWriteRepository
{
    private readonly SqlConnectionContext _connectionContext;
    public ProductWriteRepository(SqlConnectionContext connectionContext)
    {
        _connectionContext = connectionContext;
    }
    public async Task<Product?> FindByIdAsync(int productId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, Sku, Name, Description, Price, Stock, CategoryId, IsActive, CreatedAt, UpdatedAt
            FROM dbo.Products
            WHERE Id = @ProductId;
            """;

        return await QuerySingleOrDefaultAsync<Product>(sql, new { ProductId = productId }, cancellationToken);
    }
    public async Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Products WHERE Sku = @Sku) THEN 1 ELSE 0 END;";

        return await QuerySingleOrDefaultAsync<bool>(sql, new { Sku = sku }, cancellationToken);
    }
    public async Task<int> AddAsync(Product product, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Products (Sku, Name, Description, Price, Stock, CategoryId, IsActive, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Sku, @Name, @Description, @Price, @Stock, @CategoryId, @IsActive, @CreatedAt);
            """;

        return await QuerySingleOrDefaultAsync<int>(sql, product, cancellationToken);
    }
    public async Task UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Products
            SET Name = @Name, Description = @Description, Price = @Price,
                CategoryId = @CategoryId, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        await ExecuteAsync(sql, product, cancellationToken);
    }
    public async Task UpdateStockAsync(Product product, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE dbo.Products SET Stock = @Stock, UpdatedAt = @UpdatedAt WHERE Id = @Id;";
        await ExecuteAsync(sql, product, cancellationToken);
    }
    private async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(string sql, object parameters, CancellationToken cancellationToken)
    {
        DbConnection connection = await _connectionContext.GetConnectionAsync(cancellationToken);
        CommandDefinition command = new(sql, parameters, _connectionContext.CurrentTransaction, cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<TResult>(command);
    }
    private async Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        DbConnection connection = await _connectionContext.GetConnectionAsync(cancellationToken);
        CommandDefinition command = new(sql, parameters, _connectionContext.CurrentTransaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
