using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Models;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Application.InventoryMovements.Queries.GetProductMovements;
public sealed class GetProductMovementsQueryHandler : IRequestHandler<GetProductMovementsQuery, PagedResult<MovementDto>>
{
    private readonly IInventoryReadContext _readContext;
    public GetProductMovementsQueryHandler(IInventoryReadContext readContext)
    {
        _readContext = readContext;
    }
    public async Task<PagedResult<MovementDto>> Handle(GetProductMovementsQuery request, CancellationToken cancellationToken)
    {
        await EnsureProductExistsAsync(request.ProductId, cancellationToken);
        IQueryable<InventoryMovement> movements = _readContext.InventoryMovements
            .Where(movement => movement.ProductId == request.ProductId);

        return await MovementPageBuilder.BuildAsync(movements, request.Page, request.PageSize, cancellationToken);
    }
    private async Task EnsureProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        bool productExists = await _readContext.Products.AnyAsync(product => product.Id == productId, cancellationToken);
        if (!productExists)
        {
            throw new NotFoundException(nameof(Product), productId);
        }
    }
}
