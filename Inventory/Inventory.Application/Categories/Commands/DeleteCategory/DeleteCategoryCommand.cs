using MediatR;
namespace Inventory.Application.Categories.Commands.DeleteCategory;
public sealed record DeleteCategoryCommand(int CategoryId) : IRequest;
