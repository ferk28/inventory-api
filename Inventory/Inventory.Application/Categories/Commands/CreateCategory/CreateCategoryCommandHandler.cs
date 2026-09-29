using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Queries;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
namespace Inventory.Application.Categories.Commands.CreateCategory;
public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryWriteRepository _categoryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateCategoryCommandHandler(ICategoryWriteRepository categoryWriteRepository, IUnitOfWork unitOfWork)
    {
        _categoryWriteRepository = categoryWriteRepository;
        _unitOfWork = unitOfWork;
    }
    public Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(token => CreateCategoryAsync(request, token), cancellationToken);
    }
    private async Task<CategoryDto> CreateCategoryAsync(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        bool nameIsTaken = await _categoryWriteRepository.NameExistsAsync(request.Name, null, cancellationToken);
        if (nameIsTaken)
        {
            throw new ConflictException($"A category named '{request.Name}' already exists.");
        }
        Category category = new(request.Name, request.Description);
        int categoryId = await _categoryWriteRepository.AddAsync(category, cancellationToken);

        return new CategoryDto(categoryId, category.Name, category.Description, category.IsActive, category.CreatedAt);
    }
}
