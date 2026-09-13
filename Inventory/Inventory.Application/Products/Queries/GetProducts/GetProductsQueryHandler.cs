using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Models;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Application.Products.Queries.GetProducts;
public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IInventoryReadContext _readContext;
    public GetProductsQueryHandler(IInventoryReadContext readContext)
    {
        _readContext = readContext;
    }
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Product> products = ApplyFilters(_readContext.Products, request);
        int totalCount = await products.CountAsync(cancellationToken);
        List<ProductDto> items = await products
            .OrderBy(product => product.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product => new ProductDto(
                product.Id,
                product.Sku,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                product.CategoryId,
                product.Category!.Name,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, request.Page, request.PageSize, totalCount);
    }
    private static IQueryable<Product> ApplyFilters(IQueryable<Product> products, GetProductsQuery request)
    {
        if (!request.IncludeInactive)
        {
            products = products.Where(product => product.IsActive);
        }
        if (request.CategoryId is not null)
        {
            products = products.Where(product => product.CategoryId == request.CategoryId);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            products = products.Where(product => product.Name.Contains(request.Search) || product.Sku.Contains(request.Search));
        }

        return products;
    }
}
