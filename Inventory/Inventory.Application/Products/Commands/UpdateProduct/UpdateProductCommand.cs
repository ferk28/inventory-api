using MediatR;
namespace Inventory.Application.Products.Commands.UpdateProduct;
public sealed record UpdateProductCommand(int ProductId, string Name, string? Description, decimal Price, int CategoryId)
    : IRequest;
