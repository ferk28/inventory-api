using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
namespace Inventory.Application.InventoryMovements.Commands.RegisterInventoryMovement;
public sealed class RegisterInventoryMovementCommandHandler : IRequestHandler<RegisterInventoryMovementCommand, int>
{
    private readonly IProductWriteRepository _productWriteRepository;
    private readonly IInventoryMovementWriteRepository _movementWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterInventoryMovementCommandHandler(
        IProductWriteRepository productWriteRepository,
        IInventoryMovementWriteRepository movementWriteRepository,
        IUnitOfWork unitOfWork)
    {
        _productWriteRepository = productWriteRepository;
        _movementWriteRepository = movementWriteRepository;
        _unitOfWork = unitOfWork;
    }
    public Task<int> Handle(RegisterInventoryMovementCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(token => RegisterMovementAsync(request, token), cancellationToken);
    }
    private async Task<int> RegisterMovementAsync(RegisterInventoryMovementCommand request, CancellationToken cancellationToken)
    {
        Product product = await FindProductAsync(request.ProductId, cancellationToken);
        InventoryMovement movement = new(request.ProductId, request.Type, request.Quantity, request.Reason);
        product.ApplyMovement(movement);
        await _productWriteRepository.UpdateStockAsync(product, cancellationToken);

        return await _movementWriteRepository.AddAsync(movement, cancellationToken);
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
}
