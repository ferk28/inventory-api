using FluentValidation.TestHelper;
using Inventory.Application.InventoryMovements.Commands.RegisterInventoryMovement;
using Inventory.Domain.Enums;
namespace Inventory.UnitTests.Application.InventoryMovements;
public sealed class RegisterInventoryMovementCommandValidatorTests
{
    private readonly RegisterInventoryMovementCommandValidator _validator = new();
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithProductIdBelowOne_ReportsProductIdError(int productId)
    {
        RegisterInventoryMovementCommand command = new(productId, MovementType.In, 5, "restock");
        TestValidationResult<RegisterInventoryMovementCommand> result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(candidate => candidate.ProductId);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WithQuantityBelowOne_ReportsQuantityError(int quantity)
    {
        RegisterInventoryMovementCommand command = new(1, MovementType.In, quantity, "restock");
        TestValidationResult<RegisterInventoryMovementCommand> result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(candidate => candidate.Quantity);
    }
    [Fact]
    public void Validate_WithUndefinedMovementType_ReportsTypeError()
    {
        RegisterInventoryMovementCommand command = new(1, (MovementType)9, 5, "restock");
        TestValidationResult<RegisterInventoryMovementCommand> result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(candidate => candidate.Type);
    }
    [Fact]
    public void Validate_WithReasonLongerThanTheColumn_ReportsReasonError()
    {
        RegisterInventoryMovementCommand command = new(1, MovementType.In, 5, new string('x', 251));
        TestValidationResult<RegisterInventoryMovementCommand> result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(candidate => candidate.Reason);
    }
    [Fact]
    public void Validate_WithoutReason_ReportsNoReasonError()
    {
        RegisterInventoryMovementCommand command = new(1, MovementType.In, 5, null);
        TestValidationResult<RegisterInventoryMovementCommand> result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(candidate => candidate.Reason);
    }
    [Fact]
    public void Validate_WithValidCommand_ReportsNoErrors()
    {
        RegisterInventoryMovementCommand command = new(1, MovementType.Out, 3, "sale");
        TestValidationResult<RegisterInventoryMovementCommand> result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
