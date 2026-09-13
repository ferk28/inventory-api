using Inventory.Domain.Entities;
namespace Inventory.Application.Abstractions.Persistence;
public interface ICategoryWriteRepository
{
    Task<Category?> FindByIdAsync(int categoryId, CancellationToken cancellationToken);
    Task<bool> HasActiveProductsAsync(int categoryId, CancellationToken cancellationToken);
    Task DeleteAsync(int categoryId, CancellationToken cancellationToken);
}
