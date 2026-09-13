using FluentValidation;
namespace Inventory.Application.Products.Commands.UpdateProduct;
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private const int NameMaxLength = 150;
    private const int DescriptionMaxLength = 500;
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId).GreaterThan(0);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(DescriptionMaxLength);
        RuleFor(command => command.Price).GreaterThanOrEqualTo(0);
        RuleFor(command => command.CategoryId).GreaterThan(0);
    }
}
