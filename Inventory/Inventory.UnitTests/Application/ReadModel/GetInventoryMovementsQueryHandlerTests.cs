using FluentAssertions;
using Inventory.Application.Common.Models;
using Inventory.Application.InventoryMovements.Queries;
using Inventory.Application.InventoryMovements.Queries.GetInventoryMovements;
using Inventory.Domain.Enums;
namespace Inventory.UnitTests.Application.ReadModel;
public sealed class GetInventoryMovementsQueryHandlerTests : IDisposable
{
    private readonly InventoryReadDatabase _database = new();
    private readonly GetInventoryMovementsQueryHandler _handler;
    public GetInventoryMovementsQueryHandlerTests()
    {
        _handler = new GetInventoryMovementsQueryHandler(_database.Context);
    }
    [Fact]
    public async Task Handle_WithoutFilters_ReturnsEveryMovement()
    {
        PagedResult<MovementDto> result = await Handle(new GetInventoryMovementsQuery(null, null, null, null, 1, 20));
        result.TotalCount.Should().Be(3);
    }
    [Fact]
    public async Task Handle_WithProductFilter_ReturnsOnlyThatProduct()
    {
        PagedResult<MovementDto> result = await Handle(new GetInventoryMovementsQuery(1, null, null, null, 1, 20));
        result.Items.Should().OnlyContain(movement => movement.ProductId == 1);
        result.TotalCount.Should().Be(2);
    }
    [Fact]
    public async Task Handle_WithTypeFilter_ReturnsOnlyThatType()
    {
        PagedResult<MovementDto> result = await Handle(new GetInventoryMovementsQuery(null, MovementType.Out, null, null, 1, 20));
        result.Items.Should().OnlyContain(movement => movement.Type == MovementType.Out);
        result.TotalCount.Should().Be(1);
    }
    [Fact]
    public async Task Handle_WithFutureFromDate_ReturnsNothing()
    {
        PagedResult<MovementDto> result = await Handle(
            new GetInventoryMovementsQuery(null, null, DateTime.UtcNow.AddDays(1), null, 1, 20));
        result.TotalCount.Should().Be(0);
    }
    [Fact]
    public async Task Handle_WithPastToDate_ReturnsNothing()
    {
        PagedResult<MovementDto> result = await Handle(
            new GetInventoryMovementsQuery(null, null, null, DateTime.UtcNow.AddDays(-1), 1, 20));
        result.TotalCount.Should().Be(0);
    }
    [Fact]
    public async Task Handle_ReturnsTheNewestMovementFirst()
    {
        PagedResult<MovementDto> result = await Handle(new GetInventoryMovementsQuery(null, null, null, null, 1, 20));
        result.Items.Select(movement => movement.Id).Should().BeInDescendingOrder();
    }
    [Fact]
    public async Task Handle_MapsTheProductSkuAndTheStockLeftBehind()
    {
        PagedResult<MovementDto> result = await Handle(new GetInventoryMovementsQuery(1, MovementType.Out, null, null, 1, 20));
        MovementDto sale = result.Items.Single();
        sale.ProductSku.Should().Be("SKU-001");
        sale.StockAfter.Should().Be(25);
    }
    [Fact]
    public async Task Handle_WithPageSizeSmallerThanTheHistory_PagesWithoutLosingTheCount()
    {
        PagedResult<MovementDto> result = await Handle(new GetInventoryMovementsQuery(null, null, null, null, 1, 2));
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
    }
    private Task<PagedResult<MovementDto>> Handle(GetInventoryMovementsQuery query)
    {
        return _handler.Handle(query, CancellationToken.None);
    }
    public void Dispose()
    {
        _database.Dispose();
    }
}
