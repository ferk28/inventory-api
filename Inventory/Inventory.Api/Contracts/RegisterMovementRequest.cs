using Inventory.Domain.Enums;
namespace Inventory.Api.Contracts;
public sealed record RegisterMovementRequest(int ProductId, MovementType Type, int Quantity, string? Reason);
