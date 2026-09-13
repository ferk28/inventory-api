using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
namespace Inventory.Application.Products.Commands.DeleteProduct;
public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductWriteRepository _productWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteProductCommandHandler(IProductWriteRepository productWriteRepository, IUnitOfWork unitOfWork)
    {
        _productWriteRepository = productWriteRepository;
        _unitOfWork = unitOfWork;
    }
    public Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(token => DeactivateProductAsync(request, token), cancellationToken);
    }
    private async Task DeactivateProductAsync(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _productWriteRepository.FindByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }
        product.Deactivate();
        await _productWriteRepository.UpdateAsync(product, cancellationToken);
    }
}
