using FluentAssertions;
using Inventory.Application.Common.Models;
using Inventory.Application.Products.Queries;
using Inventory.Application.Products.Queries.GetProducts;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace Inventory.UnitTests.Application.Products;
public sealed class GetProductsQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Filename=:memory:");
    private readonly InventoryDbContext _context;
    private readonly GetProductsQueryHandler _handler;
    public GetProductsQueryHandlerTests()
    {
        _connection.Open();
        DbContextOptions<InventoryDbContext> options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new InventoryDbContext(options);
        _context.Database.EnsureCreated();
        SeedCatalog();
        _handler = new GetProductsQueryHandler(_context);
    }
    [Fact]
    public async Task Handle_ByDefault_ExcludesInactiveProducts()
    {
        GetProductsQuery query = new(null, null, false, 1, 20);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.Items.Should().OnlyContain(product => product.IsActive);
        result.TotalCount.Should().Be(3);
    }
    [Fact]
    public async Task Handle_WithIncludeInactive_ReturnsEveryProduct()
    {
        GetProductsQuery query = new(null, null, true, 1, 20);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(4);
    }
    [Fact]
    public async Task Handle_WithCategoryFilter_ReturnsOnlyThatCategory()
    {
        GetProductsQuery query = new(1, null, false, 1, 20);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.Items.Should().OnlyContain(product => product.CategoryId == 1);
        result.TotalCount.Should().Be(2);
    }
    [Fact]
    public async Task Handle_WithSearchMatchingTheName_FiltersByName()
    {
        GetProductsQuery query = new(null, "keyboard", false, 1, 20);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.Items.Should().ContainSingle().Which.Name.Should().Be("Mechanical keyboard");
    }
    [Fact]
    public async Task Handle_WithSearchMatchingTheSku_FiltersBySku()
    {
        GetProductsQuery query = new(null, "SKU-003", false, 1, 20);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.Items.Should().ContainSingle().Which.Sku.Should().Be("SKU-003");
    }
    [Fact]
    public async Task Handle_WithPageSizeSmallerThanTheCatalog_ReturnsOnePageAndTheFullCount()
    {
        GetProductsQuery query = new(null, null, false, 1, 2);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
    }
    [Fact]
    public async Task Handle_WithSecondPage_SkipsTheFirstPage()
    {
        GetProductsQuery query = new(null, null, false, 2, 2);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.Items.Should().ContainSingle();
        result.Page.Should().Be(2);
    }
    [Fact]
    public async Task Handle_ForEveryProduct_ResolvesTheCategoryName()
    {
        GetProductsQuery query = new(null, null, false, 1, 20);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.Items.Should().OnlyContain(product => product.CategoryName != null && product.CategoryName != "");
    }
    [Fact]
    public async Task Handle_ForAProductWithMovements_ReturnsTheCurrentStock()
    {
        GetProductsQuery query = new(null, "SKU-001", false, 1, 20);
        PagedResult<ProductDto> result = await _handler.Handle(query, CancellationToken.None);
        result.Items.Should().ContainSingle().Which.Stock.Should().Be(10);
    }
    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
    private void SeedCatalog()
    {
        Category peripherals = new("Peripherals", "Keyboards, mice and the like");
        Category storage = new("Storage", "Disks and memory cards");
        _context.AddRange(peripherals, storage);
        _context.SaveChanges();
        Product mouse = new("SKU-001", "Wireless mouse", "Ergonomic wireless mouse", 19.99m, peripherals.Id);
        mouse.ApplyMovement(new InventoryMovement(mouse.Id, MovementType.In, 10, "initial load"));
        Product keyboard = new("SKU-002", "Mechanical keyboard", "Blue switches", 59.90m, peripherals.Id);
        Product disk = new("SKU-003", "External disk", "1 TB portable disk", 74.00m, storage.Id);
        Product discontinued = new("SKU-004", "Ball mouse", "Discontinued model", 4.99m, peripherals.Id);
        discontinued.Deactivate();
        _context.AddRange(mouse, keyboard, disk, discontinued);
        _context.SaveChanges();
    }
}
