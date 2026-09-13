using FluentAssertions;
using FluentAssertions.Specialized;
using FluentValidation;
using Inventory.Application.Common.Behaviors;
using Inventory.Application.InventoryMovements.Commands.RegisterInventoryMovement;
using Inventory.Domain.Enums;
using MediatR;
namespace Inventory.UnitTests.Application.Common;
public sealed class ValidationBehaviorTests
{
    private readonly RegisterInventoryMovementCommand _validCommand = new(1, MovementType.In, 5, "restock");
    private readonly RegisterInventoryMovementCommand _invalidCommand = new(0, MovementType.In, 0, null);
    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationException()
    {
        ValidationBehavior<RegisterInventoryMovementCommand, int> behavior = CreateBehaviorWithValidator();
        Func<Task> handle = () => behavior.Handle(_invalidCommand, SucceedingNext, CancellationToken.None);
        await handle.Should().ThrowAsync<ValidationException>();
    }
    [Fact]
    public async Task Handle_WithInvalidRequest_ReportsEveryBrokenRule()
    {
        ValidationBehavior<RegisterInventoryMovementCommand, int> behavior = CreateBehaviorWithValidator();
        Func<Task> handle = () => behavior.Handle(_invalidCommand, SucceedingNext, CancellationToken.None);
        ExceptionAssertions<ValidationException> thrown = await handle.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().HaveCount(2);
    }
    [Fact]
    public async Task Handle_WithInvalidRequest_DoesNotReachTheHandler()
    {
        ValidationBehavior<RegisterInventoryMovementCommand, int> behavior = CreateBehaviorWithValidator();
        bool handlerWasReached = false;
        Task<int> Next(CancellationToken cancellationToken)
        {
            handlerWasReached = true;

            return Task.FromResult(1);
        }
        Func<Task> handle = () => behavior.Handle(_invalidCommand, Next, CancellationToken.None);
        await handle.Should().ThrowAsync<ValidationException>();
        handlerWasReached.Should().BeFalse();
    }
    [Fact]
    public async Task Handle_WithValidRequest_ReturnsTheHandlerResult()
    {
        ValidationBehavior<RegisterInventoryMovementCommand, int> behavior = CreateBehaviorWithValidator();
        int result = await behavior.Handle(_validCommand, SucceedingNext, CancellationToken.None);
        result.Should().Be(42);
    }
    [Fact]
    public async Task Handle_WithoutValidators_ReturnsTheHandlerResult()
    {
        ValidationBehavior<RegisterInventoryMovementCommand, int> behavior = new([]);
        int result = await behavior.Handle(_invalidCommand, SucceedingNext, CancellationToken.None);
        result.Should().Be(42);
    }
    private static ValidationBehavior<RegisterInventoryMovementCommand, int> CreateBehaviorWithValidator()
    {
        return new ValidationBehavior<RegisterInventoryMovementCommand, int>([new RegisterInventoryMovementCommandValidator()]);
    }
    private static Task<int> SucceedingNext(CancellationToken cancellationToken)
    {
        return Task.FromResult(42);
    }
}
