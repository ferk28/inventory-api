using Inventory.Application.Common.Models;
using Inventory.Application.InventoryMovements.Queries;
using MediatR;
namespace Inventory.Application.InventoryMovements.Queries.GetProductMovements;
public sealed record GetProductMovementsQuery(int ProductId, int Page, int PageSize) : IRequest<PagedResult<MovementDto>>;
