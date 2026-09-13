using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
namespace Inventory.UnitTests.Domain.Entities;
public sealed class ProductTests
{
    [Fact]
    public void ApplyMovement_WithInMovement_IncreasesStock()
    {
        Product product = CreateProduct();
        InventoryMovement movement = new(product.Id, MovementType.In, 10, "initial load");
        product.ApplyMovement(movement);
        product.Stock.Should().Be(10);
    }
    [Fact]
    public void ApplyMovement_WithOutMovement_DecreasesStock()
    {
        Product product = CreateProductWithStock(10);
        InventoryMovement movement = new(product.Id, MovementType.Out, 4, "sale");
        product.ApplyMovement(movement);
        product.Stock.Should().Be(6);
    }
    [Fact]
    public void ApplyMovement_WithOutMovementEqualToStock_LeavesStockAtZero()
    {
        Product product = CreateProductWithStock(5);
        InventoryMovement movement = new(product.Id, MovementType.Out, 5, "sale");
        product.ApplyMovement(movement);
        product.Stock.Should().Be(0);
    }
    [Fact]
    public void ApplyMovement_WithOutMovementGreaterThanStock_ThrowsInsufficientStockException()
    {
        Product product = CreateProductWithStock(3);
        InventoryMovement movement = new(product.Id, MovementType.Out, 4, "sale");
        Action applyMovement = () => product.ApplyMovement(movement);
        applyMovement.Should().Throw<InsufficientStockException>()
            .Which.AvailableStock.Should().Be(3);
    }
    [Fact]
    public void ApplyMovement_WithOutMovementGreaterThanStock_KeepsStockUnchanged()
    {
        Product product = CreateProductWithStock(3);
        InventoryMovement movement = new(product.Id, MovementType.Out, 4, "sale");
        Action applyMovement = () => product.ApplyMovement(movement);
        applyMovement.Should().Throw<InsufficientStockException>();
        product.Stock.Should().Be(3);
    }
    [Fact]
    public void ApplyMovement_OnInactiveProduct_ThrowsProductInactiveException()
    {
        Product product = CreateProductWithStock(10);
        product.Deactivate();
        InventoryMovement movement = new(product.Id, MovementType.In, 5, "restock");
        Action applyMovement = () => product.ApplyMovement(movement);
        applyMovement.Should().Throw<ProductInactiveException>();
    }
    [Fact]
    public void ApplyMovement_OnInactiveProduct_KeepsStockUnchanged()
    {
        Product product = CreateProductWithStock(10);
        product.Deactivate();
        InventoryMovement movement = new(product.Id, MovementType.Out, 2, "sale");
        Action applyMovement = () => product.ApplyMovement(movement);
        applyMovement.Should().Throw<ProductInactiveException>();
        product.Stock.Should().Be(10);
    }
    [Fact]
    public void ApplyMovement_WithInMovement_StampsUpdatedAt()
    {
        Product product = CreateProduct();
        InventoryMovement movement = new(product.Id, MovementType.In, 1, "restock");
        product.ApplyMovement(movement);
        product.UpdatedAt.Should().NotBeNull();
    }
    [Fact]
    public void NewProduct_StartsWithoutStockAndActive()
    {
        Product product = CreateProduct();
        product.Stock.Should().Be(0);
        product.IsActive.Should().BeTrue();
    }
    [Fact]
    public void ApplyMovement_WithInMovement_StampsTheResultingStockOnTheMovement()
    {
        Product product = CreateProductWithStock(10);
        InventoryMovement movement = new(product.Id, MovementType.In, 5, "restock");
        product.ApplyMovement(movement);
        movement.StockAfter.Should().Be(15);
    }
    [Fact]
    public void ApplyMovement_WithOutMovement_StampsTheResultingStockOnTheMovement()
    {
        Product product = CreateProductWithStock(10);
        InventoryMovement movement = new(product.Id, MovementType.Out, 4, "sale");
        product.ApplyMovement(movement);
        movement.StockAfter.Should().Be(6);
    }
    [Fact]
    public void ApplyMovement_WhenItIsRejected_LeavesTheMovementUnstamped()
    {
        Product product = CreateProductWithStock(3);
        InventoryMovement movement = new(product.Id, MovementType.Out, 4, "sale");
        Action applyMovement = () => product.ApplyMovement(movement);
        applyMovement.Should().Throw<InsufficientStockException>();
        movement.StockAfter.Should().BeNull();
    }
    private static Product CreateProduct()
    {
        return new Product("SKU-001", "Wireless mouse", "Ergonomic wireless mouse", 19.99m, 1);
    }
    private static Product CreateProductWithStock(int stock)
    {
        Product product = CreateProduct();
        product.ApplyMovement(new InventoryMovement(product.Id, MovementType.In, stock, "initial load"));

        return product;
    }
}
