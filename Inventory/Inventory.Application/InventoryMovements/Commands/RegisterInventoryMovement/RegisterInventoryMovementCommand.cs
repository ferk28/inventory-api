using Inventory.Domain.Enums;
using MediatR;
namespace Inventory.Application.InventoryMovements.Commands.RegisterInventoryMovement;
public sealed record RegisterInventoryMovementCommand(int ProductId, MovementType Type, int Quantity, string? Reason)
    : IRequest<int>;
