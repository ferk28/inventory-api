using Inventory.Domain.Entities;
namespace Inventory.Application.Abstractions.Persistence;
public interface IProductWriteRepository
{
    Task<Product?> FindByIdAsync(int productId, CancellationToken cancellationToken);
    Task UpdateStockAsync(Product product, CancellationToken cancellationToken);
}
