using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Application.Products.Queries.GetProductById;
public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IInventoryReadContext _readContext;
    public GetProductByIdQueryHandler(IInventoryReadContext readContext)
    {
        _readContext = readContext;
    }
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        ProductDto? product = await _readContext.Products
            .Where(candidate => candidate.Id == request.ProductId)
            .Select(candidate => new ProductDto(
                candidate.Id,
                candidate.Sku,
                candidate.Name,
                candidate.Description,
                candidate.Price,
                candidate.Stock,
                candidate.CategoryId,
                candidate.Category!.Name,
                candidate.IsActive,
                candidate.CreatedAt,
                candidate.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        return product;
    }
}
