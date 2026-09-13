using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Application.InventoryMovements.Queries.GetMovementById;
public sealed class GetMovementByIdQueryHandler : IRequestHandler<GetMovementByIdQuery, MovementDto>
{
    private readonly IInventoryReadContext _readContext;
    public GetMovementByIdQueryHandler(IInventoryReadContext readContext)
    {
        _readContext = readContext;
    }
    public async Task<MovementDto> Handle(GetMovementByIdQuery request, CancellationToken cancellationToken)
    {
        MovementDto? movement = await _readContext.InventoryMovements
            .Where(candidate => candidate.Id == request.MovementId)
            .Select(candidate => new MovementDto(
                candidate.Id,
                candidate.ProductId,
                candidate.Product!.Sku,
                candidate.Type,
                candidate.Quantity,
                candidate.Reason,
                candidate.StockAfter,
                candidate.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        if (movement is null)
        {
            throw new NotFoundException(nameof(InventoryMovement), request.MovementId);
        }

        return movement;
    }
}
