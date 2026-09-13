using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
namespace Inventory.Domain.Entities;
public class Product
{
    public int Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private Product()
    {
    }
    public Product(string sku, string name, string? description, decimal price, int categoryId)
    {
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        CategoryId = categoryId;
        Stock = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
    public void UpdateDetails(string name, string? description, decimal price, int categoryId)
    {
        Name = name;
        Description = description;
        Price = price;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
    public void ApplyMovement(InventoryMovement movement)
    {
        EnsureProductIsActive();
        if (movement.Type == MovementType.Out)
        {
            EnsureStockIsEnough(movement.Quantity);
            Stock -= movement.Quantity;
        }
        else
        {
            Stock += movement.Quantity;
        }
        movement.RecordStockAfter(Stock);
        UpdatedAt = DateTime.UtcNow;
    }
    private void EnsureProductIsActive()
    {
        if (!IsActive)
        {
            throw new ProductInactiveException(Id);
        }
    }
    private void EnsureStockIsEnough(int quantity)
    {
        if (quantity > Stock)
        {
            throw new InsufficientStockException(Id, Stock, quantity);
        }
    }
}
