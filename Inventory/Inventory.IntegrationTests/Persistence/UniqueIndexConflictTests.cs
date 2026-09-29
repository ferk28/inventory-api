using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
namespace Inventory.IntegrationTests.Persistence;
// The handlers' check-then-insert can be raced by a concurrent request. These tests skip the
// check and write the duplicate straight into SQL Server, so the unique index is what fires.
// Each one runs inside a unit of work that ends in the conflict, so nothing is left behind.
public sealed class UniqueIndexConflictTests : IAsyncLifetime
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
    public async Task ProductAddAsync_WithDuplicateSku_ThrowsConflictNamingTheSku()
    {
        string sku = NewUniqueValue("ITEST");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IProductWriteRepository products = scope.ServiceProvider.GetRequiredService<IProductWriteRepository>();
        Func<Task> duplicateInsert = () => unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await products.AddAsync(NewProduct(sku), token);
                await products.AddAsync(NewProduct(sku), token);
            },
            CancellationToken.None);
        await duplicateInsert.Should().ThrowAsync<ConflictException>().WithMessage($"*SKU '{sku}'*");
    }
    [Fact]
    public async Task CategoryAddAsync_WithDuplicateName_ThrowsConflictNamingTheName()
    {
        string name = NewUniqueValue("ITEST-CAT");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        ICategoryWriteRepository categories = scope.ServiceProvider.GetRequiredService<ICategoryWriteRepository>();
        Func<Task> duplicateInsert = () => unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await categories.AddAsync(new Category(name, null), token);
                await categories.AddAsync(new Category(name, null), token);
            },
            CancellationToken.None);
        await duplicateInsert.Should().ThrowAsync<ConflictException>().WithMessage($"*named '{name}'*");
    }
    [Fact]
    public async Task CategoryUpdateAsync_RenamingOntoAnExistingName_ThrowsConflictNamingTheName()
    {
        string takenName = NewUniqueValue("ITEST-CAT");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        ICategoryWriteRepository categories = scope.ServiceProvider.GetRequiredService<ICategoryWriteRepository>();
        Func<Task> collidingRename = () => unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await categories.AddAsync(new Category(takenName, null), token);
                int otherId = await categories.AddAsync(new Category(NewUniqueValue("ITEST-CAT"), null), token);
                Category other = (await categories.FindByIdAsync(otherId, token))!;
                other.Rename(takenName, null);
                await categories.UpdateAsync(other, token);
            },
            CancellationToken.None);
        await collidingRename.Should().ThrowAsync<ConflictException>().WithMessage($"*named '{takenName}'*");
    }
    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
    }
    private Product NewProduct(string sku)
    {
        return new Product(sku, "Integration probe", "Created by an integration test", 1.00m, _categoryId);
    }
    private static string NewUniqueValue(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..30];
    }
}
