using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Domain.Entities;
using NSubstitute;
namespace Inventory.UnitTests.Application.Products;
public sealed class CreateProductCommandHandlerTests
{
    private readonly IProductWriteRepository _productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductCommandHandler _handler;
    private readonly CreateProductCommand _command = new("SKU-001", "Wireless mouse", "Ergonomic wireless mouse", 19.99m, 1);
    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(_productWriteRepository, _categoryWriteRepository, _unitOfWork);
    }
    [Fact]
    public async Task Handle_WithValidCommand_PersistsTheProduct()
    {
        GivenCategory(CreateActiveCategory());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.Received(1).AddAsync(
            Arg.Is<Product>(product => product.Sku == "SKU-001" && product.Price == 19.99m && product.CategoryId == 1),
            Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithValidCommand_StartsTheProductActiveAndWithoutStock()
    {
        GivenCategory(CreateActiveCategory());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.Received(1).AddAsync(
            Arg.Is<Product>(product => product.Stock == 0 && product.IsActive),
            Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithValidCommand_ReturnsTheNewProductId()
    {
        GivenCategory(CreateActiveCategory());
        _productWriteRepository.AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(7);
        int productId = await _handler.Handle(_command, CancellationToken.None);
        productId.Should().Be(7);
    }
    [Fact]
    public async Task Handle_WithUnknownCategory_ThrowsNotFoundException()
    {
        GivenCategory(null);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    [Fact]
    public async Task Handle_WithUnknownCategory_PersistsNothing()
    {
        GivenCategory(null);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
        await _productWriteRepository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithDuplicatedSku_ThrowsConflictException()
    {
        GivenCategory(CreateActiveCategory());
        _productWriteRepository.SkuExistsAsync("SKU-001", Arg.Any<CancellationToken>()).Returns(true);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
    }
    [Fact]
    public async Task Handle_WithDuplicatedSku_PersistsNothing()
    {
        GivenCategory(CreateActiveCategory());
        _productWriteRepository.SkuExistsAsync("SKU-001", Arg.Any<CancellationToken>()).Returns(true);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
        await _productWriteRepository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithInactiveCategory_ThrowsConflictException()
    {
        GivenCategory(CreateInactiveCategory());
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
    }
    [Fact]
    public async Task Handle_WithInactiveCategory_PersistsNothing()
    {
        GivenCategory(CreateInactiveCategory());
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
        await _productWriteRepository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WhenTransactionDoesNotRun_TouchesNoRepository()
    {
        _categoryWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(CreateActiveCategory());
        await _handler.Handle(_command, CancellationToken.None);
        await _productWriteRepository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
    private void GivenCategory(Category? category)
    {
        RunTransactionInline();
        _categoryWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(category);
    }
    private static Category CreateActiveCategory()
    {
        return new Category("Electronics", "Devices and accessories");
    }
    private static Category CreateInactiveCategory()
    {
        Category category = CreateActiveCategory();
        category.Deactivate();

        return category;
    }
    private void RunTransactionInline()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<int>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<int>>>().Invoke(call.Arg<CancellationToken>()));
    }
}
