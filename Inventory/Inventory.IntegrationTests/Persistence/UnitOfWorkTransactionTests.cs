using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
namespace Inventory.IntegrationTests.Persistence;
public sealed class UnitOfWorkTransactionTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider = DatabaseScopeFactory.CreateProvider();
    private int _categoryId;
    public async Task InitializeAsync()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        ICategoryWriteRepository categories = scope.ServiceProvider.GetRequiredService<ICategoryWriteRepository>();
        Category? seeded = await categories.FindByIdAsync(1, CancellationToken.None);
        seeded.Should().NotBeNull("db/init.sql seeds at least one category");
        _categoryId = seeded!.Id;
    }
    [Fact]
    public async Task ExecuteInTransactionAsync_WhenTheOperationFails_RollsBackEveryWrite()
    {
        string sku = NewSku();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IProductWriteRepository products = scope.ServiceProvider.GetRequiredService<IProductWriteRepository>();
        IInventoryMovementWriteRepository movements = scope.ServiceProvider.GetRequiredService<IInventoryMovementWriteRepository>();
        Func<Task> failingUnitOfWork = () => unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                int productId = await products.AddAsync(NewProduct(sku), token);
                await movements.AddAsync(new InventoryMovement(productId, MovementType.In, 5, "integration test"), token);
                throw new InvalidOperationException("forced failure after both writes");
            },
            CancellationToken.None);
        await failingUnitOfWork.Should().ThrowAsync<InvalidOperationException>();
        await using AsyncServiceScope verification = _provider.CreateAsyncScope();
        IProductWriteRepository freshProducts = verification.ServiceProvider.GetRequiredService<IProductWriteRepository>();
        bool survived = await freshProducts.SkuExistsAsync(sku, CancellationToken.None);
        survived.Should().BeFalse("the transaction was rolled back, so neither write may survive");
    }
    [Fact]
    public async Task WriteRepositories_ShareTheConnectionOpenedByTheUnitOfWork()
    {
        string sku = NewSku();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IProductWriteRepository products = scope.ServiceProvider.GetRequiredService<IProductWriteRepository>();
        bool visibleInsideTheTransaction = false;
        Func<Task> failingUnitOfWork = () => unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await products.AddAsync(NewProduct(sku), token);
                visibleInsideTheTransaction = await products.SkuExistsAsync(sku, token);
                throw new InvalidOperationException("rollback so the probe leaves nothing behind");
            },
            CancellationToken.None);
        await failingUnitOfWork.Should().ThrowAsync<InvalidOperationException>();
        visibleInsideTheTransaction.Should().BeTrue(
            "a second repository read only sees the uncommitted insert when both share one connection and transaction");
    }
    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
    }
    private Product NewProduct(string sku)
    {
        return new Product(sku, "Integration probe", "Created by an integration test", 1.00m, _categoryId);
    }
    private static string NewSku()
    {
        return $"ITEST-{Guid.NewGuid():N}"[..20];
    }
}
