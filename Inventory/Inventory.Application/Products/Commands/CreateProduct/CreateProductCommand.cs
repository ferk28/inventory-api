using Inventory.Application.Products.Queries;
using MediatR;
namespace Inventory.Application.Products.Commands.CreateProduct;
public sealed record CreateProductCommand(string Sku, string Name, string? Description, decimal Price, int CategoryId)
    : IRequest<ProductDto>;
