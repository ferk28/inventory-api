using Inventory.Domain.Enums;
namespace Inventory.Domain.Entities;
public class InventoryMovement
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public MovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public int? StockAfter { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private InventoryMovement()
    {
    }
    public InventoryMovement(int productId, MovementType type, int quantity, string? reason)
    {
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        Reason = reason;
        CreatedAt = DateTime.UtcNow;
    }
    internal void RecordStockAfter(int stock)
    {
        StockAfter = stock;
    }
}
