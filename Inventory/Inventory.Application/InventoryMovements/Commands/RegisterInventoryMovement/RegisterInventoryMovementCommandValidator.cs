using FluentValidation;
using Inventory.Domain.Enums;
namespace Inventory.Application.InventoryMovements.Commands.RegisterInventoryMovement;
public sealed class RegisterInventoryMovementCommandValidator : AbstractValidator<RegisterInventoryMovementCommand>
{
    private const int ReasonMaxLength = 250;
    public RegisterInventoryMovementCommandValidator()
    {
        RuleFor(command => command.ProductId).GreaterThan(0);
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Quantity).GreaterThan(0);
        RuleFor(command => command.Reason).MaximumLength(ReasonMaxLength);
    }
}
