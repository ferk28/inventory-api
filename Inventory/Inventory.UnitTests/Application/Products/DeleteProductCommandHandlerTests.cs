using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Domain.Entities;
using NSubstitute;
namespace Inventory.UnitTests.Application.Products;
public sealed class DeleteProductCommandHandlerTests
{
    private readonly IProductWriteRepository _productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteProductCommandHandler _handler;
    private readonly DeleteProductCommand _command = new(1);
    public DeleteProductCommandHandlerTests()
    {
        _handler = new DeleteProductCommandHandler(_productWriteRepository, _unitOfWork);
    }
    [Fact]
    public async Task Handle_WithExistingProduct_DeactivatesItInsteadOfRemovingIt()
    {
        GivenProduct(CreateProduct());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Product>(product => !product.IsActive), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithExistingProduct_KeepsStockForTheHistory()
    {
        GivenProduct(CreateProduct());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Product>(product => product.Sku == "SKU-001"), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithUnknownProduct_ThrowsNotFoundException()
    {
        RunTransactionInline();
        _productWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    [Fact]
    public async Task Handle_WithAlreadyInactiveProduct_StaysIdempotent()
    {
        Product product = CreateProduct();
        product.Deactivate();
        GivenProduct(product);
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Product>(inactive => !inactive.IsActive), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WhenTransactionDoesNotRun_TouchesNoRepository()
    {
        _productWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(CreateProduct());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
    private void GivenProduct(Product product)
    {
        RunTransactionInline();
        _productWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(product);
    }
    private void RunTransactionInline()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>().Invoke(call.Arg<CancellationToken>()));
    }
    private static Product CreateProduct()
    {
        return new Product("SKU-001", "Wireless mouse", "Ergonomic wireless mouse", 19.99m, 1);
    }
}
