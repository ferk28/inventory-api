using FluentAssertions;
using Inventory.Application.Categories.Queries;
using Inventory.Application.Categories.Queries.GetCategories;
namespace Inventory.UnitTests.Application.ReadModel;
public sealed class GetCategoriesQueryHandlerTests : IDisposable
{
    private readonly InventoryReadDatabase _database = new();
    private readonly GetCategoriesQueryHandler _handler;
    public GetCategoriesQueryHandlerTests()
    {
        _handler = new GetCategoriesQueryHandler(_database.Context);
    }
    [Fact]
    public async Task Handle_ByDefault_ExcludesInactiveCategories()
    {
        IReadOnlyCollection<CategoryDto> categories = await _handler.Handle(new GetCategoriesQuery(false), CancellationToken.None);
        categories.Should().HaveCount(2);
        categories.Should().OnlyContain(category => category.IsActive);
    }
    [Fact]
    public async Task Handle_WithIncludeInactive_ReturnsEveryCategory()
    {
        IReadOnlyCollection<CategoryDto> categories = await _handler.Handle(new GetCategoriesQuery(true), CancellationToken.None);
        categories.Should().HaveCount(3);
    }
    [Fact]
    public async Task Handle_ReturnsCategoriesOrderedByName()
    {
        IReadOnlyCollection<CategoryDto> categories = await _handler.Handle(new GetCategoriesQuery(false), CancellationToken.None);
        categories.Select(category => category.Name).Should().BeInAscendingOrder();
    }
    [Fact]
    public async Task Handle_MapsEveryFieldOfTheDto()
    {
        IReadOnlyCollection<CategoryDto> categories = await _handler.Handle(new GetCategoriesQuery(false), CancellationToken.None);
        CategoryDto peripherals = categories.Single(category => category.Name == "Peripherals");
        peripherals.Id.Should().BeGreaterThan(0);
        peripherals.Description.Should().Be("Keyboards, mice and the like");
        peripherals.CreatedAt.Should().NotBe(default);
    }
    public void Dispose()
    {
        _database.Dispose();
    }
}
