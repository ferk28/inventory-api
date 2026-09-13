using FluentAssertions;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Products.Queries;
using Inventory.Application.Products.Queries.GetProductById;
namespace Inventory.UnitTests.Application.ReadModel;
public sealed class GetProductByIdQueryHandlerTests : IDisposable
{
    private readonly InventoryReadDatabase _database = new();
    private readonly GetProductByIdQueryHandler _handler;
    public GetProductByIdQueryHandlerTests()
    {
        _handler = new GetProductByIdQueryHandler(_database.Context);
    }
    [Fact]
    public async Task Handle_WithExistingProduct_ReturnsItWithTheCategoryName()
    {
        ProductDto product = await _handler.Handle(new GetProductByIdQuery(1), CancellationToken.None);
        product.Sku.Should().Be("SKU-001");
        product.CategoryName.Should().Be("Peripherals");
    }
    [Fact]
    public async Task Handle_WithExistingProduct_ReturnsTheStockLeftByItsMovements()
    {
        ProductDto product = await _handler.Handle(new GetProductByIdQuery(1), CancellationToken.None);
        product.Stock.Should().Be(25);
    }
    [Fact]
    public async Task Handle_WithInactiveProduct_StillReturnsIt()
    {
        ProductDto product = await _handler.Handle(new GetProductByIdQuery(4), CancellationToken.None);
        product.IsActive.Should().BeFalse();
    }
    [Fact]
    public async Task Handle_WithUnknownProduct_ThrowsNotFoundException()
    {
        Func<Task> handle = () => _handler.Handle(new GetProductByIdQuery(999), CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    public void Dispose()
    {
        _database.Dispose();
    }
}
