using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Models;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Application.InventoryMovements.Queries.GetInventoryMovements;
public sealed class GetInventoryMovementsQueryHandler : IRequestHandler<GetInventoryMovementsQuery, PagedResult<MovementDto>>
{
    private readonly IInventoryReadContext _readContext;
    public GetInventoryMovementsQueryHandler(IInventoryReadContext readContext)
    {
        _readContext = readContext;
    }
    public async Task<PagedResult<MovementDto>> Handle(GetInventoryMovementsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<InventoryMovement> movements = ApplyFilters(_readContext.InventoryMovements, request);

        return await MovementPageBuilder.BuildAsync(movements, request.Page, request.PageSize, cancellationToken);
    }
    private static IQueryable<InventoryMovement> ApplyFilters(IQueryable<InventoryMovement> movements, GetInventoryMovementsQuery request)
    {
        if (request.ProductId is not null)
        {
            movements = movements.Where(movement => movement.ProductId == request.ProductId);
        }
        if (request.Type is not null)
        {
            movements = movements.Where(movement => movement.Type == request.Type);
        }
        if (request.From is not null)
        {
            movements = movements.Where(movement => movement.CreatedAt >= request.From);
        }
        if (request.To is not null)
        {
            movements = movements.Where(movement => movement.CreatedAt <= request.To);
        }

        return movements;
    }
}
