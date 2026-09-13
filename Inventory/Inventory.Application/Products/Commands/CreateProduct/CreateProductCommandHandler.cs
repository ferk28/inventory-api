using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
namespace Inventory.Application.Products.Commands.CreateProduct;
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly IProductWriteRepository _productWriteRepository;
    private readonly ICategoryWriteRepository _categoryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateProductCommandHandler(
        IProductWriteRepository productWriteRepository,
        ICategoryWriteRepository categoryWriteRepository,
        IUnitOfWork unitOfWork)
    {
        _productWriteRepository = productWriteRepository;
        _categoryWriteRepository = categoryWriteRepository;
        _unitOfWork = unitOfWork;
    }
    public Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(token => CreateProductAsync(request, token), cancellationToken);
    }
    private async Task<int> CreateProductAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureSkuIsAvailableAsync(request.Sku, cancellationToken);
        Product product = new(request.Sku, request.Name, request.Description, request.Price, request.CategoryId);

        return await _productWriteRepository.AddAsync(product, cancellationToken);
    }
    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        bool categoryExists = await _categoryWriteRepository.ExistsAsync(categoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException(nameof(Category), categoryId);
        }
    }
    private async Task EnsureSkuIsAvailableAsync(string sku, CancellationToken cancellationToken)
    {
        bool skuExists = await _productWriteRepository.SkuExistsAsync(sku, cancellationToken);
        if (skuExists)
        {
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
        }
    }
}
