using Inventory.Application.InventoryMovements.Queries;
using MediatR;
namespace Inventory.Application.InventoryMovements.Queries.GetMovementById;
public sealed record GetMovementByIdQuery(int MovementId) : IRequest<MovementDto>;
