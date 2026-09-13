namespace Inventory.Domain.Exceptions;
public sealed class InsufficientStockException : DomainException
{
    public int ProductId { get; }
    public int AvailableStock { get; }
    public int RequestedQuantity { get; }
    public InsufficientStockException(int productId, int availableStock, int requestedQuantity)
        : base($"Product {productId} has {availableStock} units in stock, cannot remove {requestedQuantity}.")
    {
        ProductId = productId;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }
}
