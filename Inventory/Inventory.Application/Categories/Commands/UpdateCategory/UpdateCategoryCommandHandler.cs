using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
namespace Inventory.Application.Categories.Commands.UpdateCategory;
public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly ICategoryWriteRepository _categoryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateCategoryCommandHandler(ICategoryWriteRepository categoryWriteRepository, IUnitOfWork unitOfWork)
    {
        _categoryWriteRepository = categoryWriteRepository;
        _unitOfWork = unitOfWork;
    }
    public Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(token => UpdateCategoryAsync(request, token), cancellationToken);
    }
    private async Task UpdateCategoryAsync(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        Category category = await FindCategoryAsync(request.CategoryId, cancellationToken);
        await EnsureNameIsAvailableAsync(request, cancellationToken);
        category.Rename(request.Name, request.Description);
        await _categoryWriteRepository.UpdateAsync(category, cancellationToken);
    }
    private async Task<Category> FindCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        Category? category = await _categoryWriteRepository.FindByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException(nameof(Category), categoryId);
        }

        return category;
    }
    private async Task EnsureNameIsAvailableAsync(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        bool nameIsTaken = await _categoryWriteRepository.NameExistsAsync(request.Name, request.CategoryId, cancellationToken);
        if (nameIsTaken)
        {
            throw new ConflictException($"A category named '{request.Name}' already exists.");
        }
    }
}
