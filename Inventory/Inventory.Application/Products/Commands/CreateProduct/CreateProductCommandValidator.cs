using FluentValidation;
namespace Inventory.Application.Products.Commands.CreateProduct;
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private const int SkuMaxLength = 50;
    private const int NameMaxLength = 150;
    private const int DescriptionMaxLength = 500;
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Sku).NotEmpty().MaximumLength(SkuMaxLength);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(DescriptionMaxLength);
        RuleFor(command => command.Price).GreaterThanOrEqualTo(0);
        RuleFor(command => command.CategoryId).GreaterThan(0);
    }
}
