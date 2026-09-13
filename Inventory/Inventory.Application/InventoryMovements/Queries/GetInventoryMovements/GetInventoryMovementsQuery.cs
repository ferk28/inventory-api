using Inventory.Application.Common.Models;
using Inventory.Application.InventoryMovements.Queries;
using Inventory.Domain.Enums;
using MediatR;
namespace Inventory.Application.InventoryMovements.Queries.GetInventoryMovements;
public sealed record GetInventoryMovementsQuery(
    int? ProductId,
    MovementType? Type,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize) : IRequest<PagedResult<MovementDto>>;
