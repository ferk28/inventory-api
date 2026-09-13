using Inventory.Domain.Entities;
namespace Inventory.Application.Abstractions.Persistence;
public interface ICategoryWriteRepository
{
    Task<Category?> FindByIdAsync(int categoryId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, int? excludedCategoryId, CancellationToken cancellationToken);
    Task<bool> HasActiveProductsAsync(int categoryId, CancellationToken cancellationToken);
    Task<int> AddAsync(Category category, CancellationToken cancellationToken);
    Task UpdateAsync(Category category, CancellationToken cancellationToken);
}
