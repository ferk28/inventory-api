using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using NSubstitute;
namespace Inventory.UnitTests.Application.Categories;
public sealed class CreateCategoryCommandHandlerTests
{
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateCategoryCommandHandler _handler;
    private readonly CreateCategoryCommand _command = new("Electronics", "Devices and accessories");
    public CreateCategoryCommandHandlerTests()
    {
        _handler = new CreateCategoryCommandHandler(_categoryWriteRepository, _unitOfWork);
    }
    [Fact]
    public async Task Handle_WithFreeName_PersistsTheCategory()
    {
        GivenNameIsTaken(false);
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.Received(1).AddAsync(
            Arg.Is<Category>(category => category.Name == "Electronics"), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithFreeName_StartsTheCategoryActive()
    {
        GivenNameIsTaken(false);
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.Received(1).AddAsync(
            Arg.Is<Category>(category => category.IsActive), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithFreeName_ReturnsTheNewCategoryId()
    {
        GivenNameIsTaken(false);
        _categoryWriteRepository.AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>()).Returns(9);
        int categoryId = await _handler.Handle(_command, CancellationToken.None);
        categoryId.Should().Be(9);
    }
    [Fact]
    public async Task Handle_WithTakenName_ThrowsConflictException()
    {
        GivenNameIsTaken(true);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
    }
    [Fact]
    public async Task Handle_WithTakenName_PersistsNothing()
    {
        GivenNameIsTaken(true);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
        await _categoryWriteRepository.DidNotReceive().AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_ChecksTheNameWithoutExcludingAnyCategory()
    {
        GivenNameIsTaken(false);
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.Received(1).NameExistsAsync("Electronics", null, Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WhenTransactionDoesNotRun_TouchesNoRepository()
    {
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.DidNotReceive().AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }
    private void GivenNameIsTaken(bool taken)
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<int>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<int>>>().Invoke(call.Arg<CancellationToken>()));
        _categoryWriteRepository.NameExistsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(taken);
    }
}
