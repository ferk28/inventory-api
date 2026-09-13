using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
namespace Inventory.Application.Categories.Commands.DeleteCategory;
public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryWriteRepository _categoryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteCategoryCommandHandler(ICategoryWriteRepository categoryWriteRepository, IUnitOfWork unitOfWork)
    {
        _categoryWriteRepository = categoryWriteRepository;
        _unitOfWork = unitOfWork;
    }
    public Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(token => DeleteCategoryAsync(request, token), cancellationToken);
    }
    private async Task DeleteCategoryAsync(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureCategoryIsEmptyAsync(request.CategoryId, cancellationToken);
        await _categoryWriteRepository.DeleteAsync(request.CategoryId, cancellationToken);
    }
    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        bool categoryExists = await _categoryWriteRepository.ExistsAsync(categoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException(nameof(Category), categoryId);
        }
    }
    private async Task EnsureCategoryIsEmptyAsync(int categoryId, CancellationToken cancellationToken)
    {
        bool hasActiveProducts = await _categoryWriteRepository.HasActiveProductsAsync(categoryId, cancellationToken);
        if (hasActiveProducts)
        {
            throw new ConflictException($"Category {categoryId} still has active products and cannot be deleted.");
        }
    }
}
