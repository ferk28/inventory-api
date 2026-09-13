using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
namespace Inventory.Application.Products.Commands.UpdateProduct;
public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IProductWriteRepository _productWriteRepository;
    private readonly ICategoryWriteRepository _categoryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateProductCommandHandler(
        IProductWriteRepository productWriteRepository,
        ICategoryWriteRepository categoryWriteRepository,
        IUnitOfWork unitOfWork)
    {
        _productWriteRepository = productWriteRepository;
        _categoryWriteRepository = categoryWriteRepository;
        _unitOfWork = unitOfWork;
    }
    public Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(token => UpdateProductAsync(request, token), cancellationToken);
    }
    private async Task UpdateProductAsync(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product product = await FindProductAsync(request.ProductId, cancellationToken);
        await EnsureCategoryAcceptsProductsAsync(request.CategoryId, cancellationToken);
        product.UpdateDetails(request.Name, request.Description, request.Price, request.CategoryId);
        await _productWriteRepository.UpdateAsync(product, cancellationToken);
    }
    private async Task<Product> FindProductAsync(int productId, CancellationToken cancellationToken)
    {
        Product? product = await _productWriteRepository.FindByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException(nameof(Product), productId);
        }

        return product;
    }
    private async Task EnsureCategoryAcceptsProductsAsync(int categoryId, CancellationToken cancellationToken)
    {
        Category? category = await _categoryWriteRepository.FindByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException(nameof(Category), categoryId);
        }
        if (!category.IsActive)
        {
            throw new ConflictException($"Category {categoryId} is inactive and cannot hold products.");
        }
    }
}
