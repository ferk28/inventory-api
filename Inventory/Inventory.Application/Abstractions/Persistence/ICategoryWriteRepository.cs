namespace Inventory.Application.Abstractions.Persistence;
public interface ICategoryWriteRepository
{
    Task<bool> ExistsAsync(int categoryId, CancellationToken cancellationToken);
}
