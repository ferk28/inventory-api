using FluentValidation;
namespace Inventory.Application.Products.Commands.DeleteProduct;
public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(command => command.ProductId).GreaterThan(0);
    }
}
