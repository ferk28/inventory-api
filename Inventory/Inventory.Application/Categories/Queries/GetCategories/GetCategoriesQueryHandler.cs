using Inventory.Application.Abstractions.Persistence;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Application.Categories.Queries.GetCategories;
public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    private readonly IInventoryReadContext _readContext;
    public GetCategoriesQueryHandler(IInventoryReadContext readContext)
    {
        _readContext = readContext;
    }
    public async Task<IReadOnlyCollection<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Category> categories = _readContext.Categories;
        if (!request.IncludeInactive)
        {
            categories = categories.Where(category => category.IsActive);
        }

        return await categories
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(
                category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
