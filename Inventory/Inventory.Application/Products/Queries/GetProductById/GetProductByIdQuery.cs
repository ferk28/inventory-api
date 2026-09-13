using Inventory.Application.Products.Queries;
using MediatR;
namespace Inventory.Application.Products.Queries.GetProductById;
public sealed record GetProductByIdQuery(int ProductId) : IRequest<ProductDto>;
