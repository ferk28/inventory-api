namespace Inventory.Application.Abstractions.Persistence;
public interface ICategoryWriteRepository
{
    Task<bool> ExistsAsync(int categoryId, CancellationToken cancellationToken);
    Task<bool> HasActiveProductsAsync(int categoryId, CancellationToken cancellationToken);
    Task DeleteAsync(int categoryId, CancellationToken cancellationToken);
}
