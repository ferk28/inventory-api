namespace Inventory.Domain.Exceptions;
public sealed class ProductInactiveException : DomainException
{
    public int ProductId { get; }
    public ProductInactiveException(int productId)
        : base($"Product {productId} is inactive and cannot receive inventory movements.")
    {
        ProductId = productId;
    }
}
