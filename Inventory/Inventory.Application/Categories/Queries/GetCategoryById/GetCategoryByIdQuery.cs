using Inventory.Application.Categories.Queries;
using MediatR;
namespace Inventory.Application.Categories.Queries.GetCategoryById;
public sealed record GetCategoryByIdQuery(int CategoryId) : IRequest<CategoryDto>;
