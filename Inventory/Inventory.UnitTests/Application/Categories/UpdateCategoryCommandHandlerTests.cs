using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using NSubstitute;
namespace Inventory.UnitTests.Application.Categories;
public sealed class UpdateCategoryCommandHandlerTests
{
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateCategoryCommandHandler _handler;
    private readonly UpdateCategoryCommand _command = new(1, "Peripherals", "Keyboards and mice");
    public UpdateCategoryCommandHandlerTests()
    {
        _handler = new UpdateCategoryCommandHandler(_categoryWriteRepository, _unitOfWork);
    }
    [Fact]
    public async Task Handle_WithFreeName_PersistsTheNewDetails()
    {
        GivenCategory(CreateCategory(), nameTaken: false);
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Category>(category => category.Name == "Peripherals" && category.Description == "Keyboards and mice"),
            Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithFreeName_LeavesTheCategoryActive()
    {
        GivenCategory(CreateCategory(), nameTaken: false);
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.Received(1).UpdateAsync(
            Arg.Is<Category>(category => category.IsActive), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_ExcludesItselfFromTheNameCheck()
    {
        GivenCategory(CreateCategory(), nameTaken: false);
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.Received(1).NameExistsAsync("Peripherals", 1, Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithUnknownCategory_ThrowsNotFoundException()
    {
        GivenCategory(null, nameTaken: false);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    [Fact]
    public async Task Handle_WithNameTakenByAnotherCategory_ThrowsConflictException()
    {
        GivenCategory(CreateCategory(), nameTaken: true);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
    }
    [Fact]
    public async Task Handle_WithNameTakenByAnotherCategory_PersistsNothing()
    {
        GivenCategory(CreateCategory(), nameTaken: true);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
        await _categoryWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WhenTransactionDoesNotRun_TouchesNoRepository()
    {
        _categoryWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(CreateCategory());
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }
    private void GivenCategory(Category? category, bool nameTaken)
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>().Invoke(call.Arg<CancellationToken>()));
        _categoryWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(category);
        _categoryWriteRepository.NameExistsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(nameTaken);
    }
    private static Category CreateCategory()
    {
        return new Category("Electronics", "Devices and accessories");
    }
}
