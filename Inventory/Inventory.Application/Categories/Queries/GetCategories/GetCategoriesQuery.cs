using Inventory.Application.Categories.Queries;
using MediatR;
namespace Inventory.Application.Categories.Queries.GetCategories;
public sealed record GetCategoriesQuery(bool IncludeInactive) : IRequest<IReadOnlyCollection<CategoryDto>>;
