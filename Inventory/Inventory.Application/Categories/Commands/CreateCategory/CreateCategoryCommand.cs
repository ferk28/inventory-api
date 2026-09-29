using Inventory.Application.Categories.Queries;
using MediatR;
namespace Inventory.Application.Categories.Commands.CreateCategory;
public sealed record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDto>;
