using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
namespace Inventory.IntegrationTests.Persistence;
public sealed class ProductWriteRepositoryTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider = DatabaseScopeFactory.CreateProvider();
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
    [Fact]
    public async Task FindByIdAsync_MaterialisesEveryFieldBehindThePrivateSetters()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IProductWriteRepository products = scope.ServiceProvider.GetRequiredService<IProductWriteRepository>();
        Product? product = await products.FindByIdAsync(1, CancellationToken.None);
        product.Should().NotBeNull("db/init.sql seeds at least one product");
        product!.Id.Should().Be(1);
        product.Sku.Should().NotBeNullOrWhiteSpace();
        product.Stock.Should().BeGreaterThanOrEqualTo(0);
        product.CategoryId.Should().BeGreaterThan(0);
        product.CreatedAt.Should().NotBe(default);
    }
    [Fact]
    public async Task FindByIdAsync_WithUnknownId_ReturnsNull()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IProductWriteRepository products = scope.ServiceProvider.GetRequiredService<IProductWriteRepository>();
        Product? product = await products.FindByIdAsync(int.MaxValue, CancellationToken.None);
        product.Should().BeNull();
    }
    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
    }
}
