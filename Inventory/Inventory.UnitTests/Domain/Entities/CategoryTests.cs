using FluentAssertions;
using Inventory.Domain.Entities;
namespace Inventory.UnitTests.Domain.Entities;
public sealed class CategoryTests
{
    [Fact]
    public void NewCategory_StartsActive()
    {
        Category category = CreateCategory();
        category.IsActive.Should().BeTrue();
    }
    [Fact]
    public void Deactivate_MakesTheCategoryInactive()
    {
        Category category = CreateCategory();
        category.Deactivate();
        category.IsActive.Should().BeFalse();
    }
    [Fact]
    public void Activate_BringsTheCategoryBack()
    {
        Category category = CreateCategory();
        category.Deactivate();
        category.Activate();
        category.IsActive.Should().BeTrue();
    }
    [Fact]
    public void Rename_ChangesNameAndDescription()
    {
        Category category = CreateCategory();
        category.Rename("Peripherals", "Keyboards and mice");
        category.Name.Should().Be("Peripherals");
        category.Description.Should().Be("Keyboards and mice");
    }
    [Fact]
    public void HasActiveProducts_OnAnEmptyCategory_ReturnsFalse()
    {
        Category category = CreateCategory();
        category.HasActiveProducts().Should().BeFalse();
    }
    private static Category CreateCategory()
    {
        return new Category("Electronics", "Devices and accessories");
    }
}
