using Inventory.Application.Common.Models;
using MediatR;
namespace Inventory.Application.Products.Queries.GetProducts;
public sealed record GetProductsQuery(int? CategoryId, string? Search, bool IncludeInactive, int Page, int PageSize)
    : IRequest<PagedResult<ProductDto>>;
