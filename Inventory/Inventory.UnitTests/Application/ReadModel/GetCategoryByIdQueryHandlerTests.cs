using FluentAssertions;
using Inventory.Application.Categories.Queries;
using Inventory.Application.Categories.Queries.GetCategoryById;
using Inventory.Application.Common.Exceptions;
namespace Inventory.UnitTests.Application.ReadModel;
public sealed class GetCategoryByIdQueryHandlerTests : IDisposable
{
    private readonly InventoryReadDatabase _database = new();
    private readonly GetCategoryByIdQueryHandler _handler;
    public GetCategoryByIdQueryHandlerTests()
    {
        _handler = new GetCategoryByIdQueryHandler(_database.Context);
    }
    [Fact]
    public async Task Handle_WithExistingCategory_ReturnsIt()
    {
        CategoryDto category = await _handler.Handle(new GetCategoryByIdQuery(1), CancellationToken.None);
        category.Id.Should().Be(1);
        category.Name.Should().Be("Peripherals");
    }
    [Fact]
    public async Task Handle_WithInactiveCategory_StillReturnsIt()
    {
        CategoryDto category = await _handler.Handle(new GetCategoryByIdQuery(3), CancellationToken.None);
        category.IsActive.Should().BeFalse();
    }
    [Fact]
    public async Task Handle_WithUnknownCategory_ThrowsNotFoundException()
    {
        Func<Task> handle = () => _handler.Handle(new GetCategoryByIdQuery(999), CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    public void Dispose()
    {
        _database.Dispose();
    }
}
