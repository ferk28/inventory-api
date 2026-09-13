using Inventory.Domain.Entities;
namespace Inventory.Application.Abstractions.Persistence;
public interface IInventoryMovementWriteRepository
{
    Task<int> AddAsync(InventoryMovement movement, CancellationToken cancellationToken);
}
