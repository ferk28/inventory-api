using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Domain.Entities;
using NSubstitute;
namespace Inventory.UnitTests.Application.Products;
public sealed class UpdateProductCommandHandlerTests
{
    private readonly IProductWriteRepository _productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateProductCommandHandler _handler;
    private readonly UpdateProductCommand _command = new(1, "Wireless mouse v2", "Updated description", 24.50m, 2);
    public UpdateProductCommandHandlerTests()
    {
        _handler = new UpdateProductCommandHandler(_productWriteRepository, _categoryWriteRepository, _unitOfWork);
    }
    [Fact]
    public async Task Handle_WithValidCommand_PersistsTheNewDetails()
    {
        GivenProductAndCategory(CreateProduct());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Product>(product => product.Name == "Wireless mouse v2" && product.Price == 24.50m && product.CategoryId == 2),
            Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithValidCommand_LeavesStockUntouched()
    {
        Product product = CreateProduct();
        GivenProductAndCategory(product);
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Product>(updated => updated.Stock == 0), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithValidCommand_StampsUpdatedAt()
    {
        GivenProductAndCategory(CreateProduct());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Product>(product => product.UpdatedAt != null), Arg.Any<CancellationToken>());
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
    public async Task Handle_WithUnknownCategory_ThrowsNotFoundException()
    {
        GivenProductAndCategory(CreateProduct(), categoryExists: false);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    [Fact]
    public async Task Handle_WithUnknownCategory_PersistsNothing()
    {
        GivenProductAndCategory(CreateProduct(), categoryExists: false);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
        await _productWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WhenTransactionDoesNotRun_TouchesNoRepository()
    {
        _productWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(CreateProduct());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
    private void GivenProductAndCategory(Product product, bool categoryExists = true)
    {
        RunTransactionInline();
        _productWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(product);
        _categoryWriteRepository.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(categoryExists);
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
