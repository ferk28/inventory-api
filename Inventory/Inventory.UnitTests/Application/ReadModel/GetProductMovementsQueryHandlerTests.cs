using FluentAssertions;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Models;
using Inventory.Application.InventoryMovements.Queries;
using Inventory.Application.InventoryMovements.Queries.GetProductMovements;
namespace Inventory.UnitTests.Application.ReadModel;
public sealed class GetProductMovementsQueryHandlerTests : IDisposable
{
    private readonly InventoryReadDatabase _database = new();
    private readonly GetProductMovementsQueryHandler _handler;
    public GetProductMovementsQueryHandlerTests()
    {
        _handler = new GetProductMovementsQueryHandler(_database.Context);
    }
    [Fact]
    public async Task Handle_WithExistingProduct_ReturnsOnlyItsMovements()
    {
        PagedResult<MovementDto> result = await _handler.Handle(new GetProductMovementsQuery(1, 1, 20), CancellationToken.None);
        result.Items.Should().OnlyContain(movement => movement.ProductId == 1);
        result.TotalCount.Should().Be(2);
    }
    [Fact]
    public async Task Handle_WithProductWithoutMovements_ReturnsAnEmptyPage()
    {
        PagedResult<MovementDto> result = await _handler.Handle(new GetProductMovementsQuery(3, 1, 20), CancellationToken.None);
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
    [Fact]
    public async Task Handle_WithUnknownProduct_ThrowsNotFoundException()
    {
        Func<Task> handle = () => _handler.Handle(new GetProductMovementsQuery(999, 1, 20), CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    public void Dispose()
    {
        _database.Dispose();
    }
}
