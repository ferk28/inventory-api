using MediatR;
namespace Inventory.Application.Categories.Commands.UpdateCategory;
public sealed record UpdateCategoryCommand(int CategoryId, string Name, string? Description) : IRequest;
