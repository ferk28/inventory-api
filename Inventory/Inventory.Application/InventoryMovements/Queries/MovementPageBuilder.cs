using Inventory.Application.Common.Models;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Application.InventoryMovements.Queries;
public static class MovementPageBuilder
{
    public static async Task<PagedResult<MovementDto>> BuildAsync(
        IQueryable<InventoryMovement> movements,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        int totalCount = await movements.CountAsync(cancellationToken);
        List<MovementDto> items = await movements
            .OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(movement => new MovementDto(
                movement.Id,
                movement.ProductId,
                movement.Product!.Sku,
                movement.Type,
                movement.Quantity,
                movement.Reason,
                movement.StockAfter,
                movement.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<MovementDto>(items, page, pageSize, totalCount);
    }
}
