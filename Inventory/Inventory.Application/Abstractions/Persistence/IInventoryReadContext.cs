using Inventory.Domain.Entities;
namespace Inventory.Application.Abstractions.Persistence;
public interface IInventoryReadContext
{
    IQueryable<Category> Categories { get; }
    IQueryable<Product> Products { get; }
    IQueryable<InventoryMovement> InventoryMovements { get; }
}
