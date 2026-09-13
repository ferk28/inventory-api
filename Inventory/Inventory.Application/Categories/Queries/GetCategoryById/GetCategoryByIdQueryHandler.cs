using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Application.Categories.Queries.GetCategoryById;
public sealed class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly IInventoryReadContext _readContext;
    public GetCategoryByIdQueryHandler(IInventoryReadContext readContext)
    {
        _readContext = readContext;
    }
    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        CategoryDto? category = await _readContext.Categories
            .Where(candidate => candidate.Id == request.CategoryId)
            .Select(candidate => new CategoryDto(
                candidate.Id, candidate.Name, candidate.Description, candidate.IsActive, candidate.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.CategoryId);
        }

        return category;
    }
}
