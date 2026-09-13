using MediatR;
namespace Inventory.Application.Products.Commands.DeleteProduct;
public sealed record DeleteProductCommand(int ProductId) : IRequest;
