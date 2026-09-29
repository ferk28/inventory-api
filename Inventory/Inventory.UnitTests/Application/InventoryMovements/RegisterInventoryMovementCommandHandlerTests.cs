using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.InventoryMovements.Commands.RegisterInventoryMovement;
using Inventory.Application.InventoryMovements.Queries;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using NSubstitute;
namespace Inventory.UnitTests.Application.InventoryMovements;
public sealed class RegisterInventoryMovementCommandHandlerTests
{
    private readonly IProductWriteRepository _productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly IInventoryMovementWriteRepository _movementWriteRepository = Substitute.For<IInventoryMovementWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterInventoryMovementCommandHandler _handler;
    public RegisterInventoryMovementCommandHandlerTests()
    {
        _handler = new RegisterInventoryMovementCommandHandler(_productWriteRepository, _movementWriteRepository, _unitOfWork);
    }
    [Fact]
    public async Task Handle_WithInMovement_IncreasesProductStock()
    {
        RunTransactionInline();
        GivenProduct(CreateProductWithStock(10));
        RegisterInventoryMovementCommand command = new(1, MovementType.In, 5, "restock");
        await _handler.Handle(command, CancellationToken.None);
        await _productWriteRepository.Received(1)
            .UpdateStockAsync(Arg.Is<Product>(product => product.Stock == 15), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithOutMovement_DecreasesProductStock()
    {
        RunTransactionInline();
        GivenProduct(CreateProductWithStock(10));
        RegisterInventoryMovementCommand command = new(1, MovementType.Out, 4, "sale");
        await _handler.Handle(command, CancellationToken.None);
        await _productWriteRepository.Received(1)
            .UpdateStockAsync(Arg.Is<Product>(product => product.Stock == 6), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithValidMovement_ReturnsTheNewMovementWithItsId()
    {
        RunTransactionInline();
        GivenProduct(CreateProductWithStock(10));
        _movementWriteRepository.AddAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>()).Returns(42);
        RegisterInventoryMovementCommand command = new(1, MovementType.In, 5, "restock");
        MovementDto movement = await _handler.Handle(command, CancellationToken.None);
        movement.Id.Should().Be(42);
        movement.ProductSku.Should().Be("SKU-001");
        movement.Quantity.Should().Be(5);
    }
    [Fact]
    public async Task Handle_WithValidMovement_ReturnsTheStockLeftAfterIt()
    {
        RunTransactionInline();
        GivenProduct(CreateProductWithStock(10));
        RegisterInventoryMovementCommand command = new(1, MovementType.Out, 4, "sale");
        MovementDto movement = await _handler.Handle(command, CancellationToken.None);
        movement.StockAfter.Should().Be(6);
    }
    [Fact]
    public async Task Handle_WithOutMovementGreaterThanStock_ThrowsInsufficientStockException()
    {
        RunTransactionInline();
        GivenProduct(CreateProductWithStock(3));
        RegisterInventoryMovementCommand command = new(1, MovementType.Out, 4, "sale");
        Func<Task> handle = () => _handler.Handle(command, CancellationToken.None);
        await handle.Should().ThrowAsync<InsufficientStockException>();
    }
    [Fact]
    public async Task Handle_WithOutMovementGreaterThanStock_DoesNotPersistAnything()
    {
        RunTransactionInline();
        GivenProduct(CreateProductWithStock(3));
        RegisterInventoryMovementCommand command = new(1, MovementType.Out, 4, "sale");
        Func<Task> handle = () => _handler.Handle(command, CancellationToken.None);
        await handle.Should().ThrowAsync<InsufficientStockException>();
        await _movementWriteRepository.DidNotReceive().AddAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithUnknownProduct_ThrowsNotFoundException()
    {
        RunTransactionInline();
        _productWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        RegisterInventoryMovementCommand command = new(99, MovementType.In, 5, "restock");
        Func<Task> handle = () => _handler.Handle(command, CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    [Fact]
    public async Task Handle_WithInactiveProduct_ThrowsProductInactiveException()
    {
        RunTransactionInline();
        Product product = CreateProductWithStock(10);
        product.Deactivate();
        GivenProduct(product);
        RegisterInventoryMovementCommand command = new(1, MovementType.In, 5, "restock");
        Func<Task> handle = () => _handler.Handle(command, CancellationToken.None);
        await handle.Should().ThrowAsync<ProductInactiveException>();
    }
    [Fact]
    public async Task Handle_WhenTransactionDoesNotRun_TouchesNoRepository()
    {
        GivenProduct(CreateProductWithStock(10));
        RegisterInventoryMovementCommand command = new(1, MovementType.In, 5, "restock");
        await _handler.Handle(command, CancellationToken.None);
        await _productWriteRepository.DidNotReceive().FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _movementWriteRepository.DidNotReceive().AddAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithValidMovement_RunsInsideASingleTransaction()
    {
        RunTransactionInline();
        GivenProduct(CreateProductWithStock(10));
        RegisterInventoryMovementCommand command = new(1, MovementType.In, 5, "restock");
        await _handler.Handle(command, CancellationToken.None);
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<MovementDto>>>(), Arg.Any<CancellationToken>());
    }
    private void RunTransactionInline()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<MovementDto>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<MovementDto>>>().Invoke(call.Arg<CancellationToken>()));
    }
    private void GivenProduct(Product product)
    {
        _productWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(product);
    }
    private static Product CreateProductWithStock(int stock)
    {
        Product product = new("SKU-001", "Wireless mouse", "Ergonomic wireless mouse", 19.99m, 1);
        product.ApplyMovement(new InventoryMovement(product.Id, MovementType.In, stock, "initial load"));

        return product;
    }
}
