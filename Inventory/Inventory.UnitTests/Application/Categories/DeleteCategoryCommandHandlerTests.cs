using FluentAssertions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using NSubstitute;
namespace Inventory.UnitTests.Application.Categories;
public sealed class DeleteCategoryCommandHandlerTests
{
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteCategoryCommandHandler _handler;
    private readonly DeleteCategoryCommand _command = new(1);
    public DeleteCategoryCommandHandlerTests()
    {
        _handler = new DeleteCategoryCommandHandler(_categoryWriteRepository, _unitOfWork);
    }
    [Fact]
    public async Task Handle_WithEmptyCategory_DeletesIt()
    {
        GivenCategory(hasActiveProducts: false);
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithActiveProducts_ThrowsConflictException()
    {
        GivenCategory(hasActiveProducts: true);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
    }
    [Fact]
    public async Task Handle_WithActiveProducts_DeletesNothing()
    {
        GivenCategory(hasActiveProducts: true);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<ConflictException>();
        await _categoryWriteRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WithUnknownCategory_ThrowsNotFoundException()
    {
        GivenCategory(hasActiveProducts: false, exists: false);
        Func<Task> handle = () => _handler.Handle(_command, CancellationToken.None);
        await handle.Should().ThrowAsync<NotFoundException>();
    }
    [Fact]
    public async Task Handle_WhenTransactionDoesNotRun_TouchesNoRepository()
    {
        _categoryWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new Category("Electronics", "Devices and accessories"));
        await _handler.Handle(_command, CancellationToken.None);
        await _categoryWriteRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
    private void GivenCategory(bool hasActiveProducts, bool exists = true)
    {
        RunTransactionInline();
        _categoryWriteRepository.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(exists ? new Category("Electronics", "Devices and accessories") : null);
        _categoryWriteRepository.HasActiveProductsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(hasActiveProducts);
    }
    private void RunTransactionInline()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>().Invoke(call.Arg<CancellationToken>()));
    }
}
