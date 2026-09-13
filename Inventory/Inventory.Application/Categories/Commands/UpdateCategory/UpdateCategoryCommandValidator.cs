using FluentValidation;
namespace Inventory.Application.Categories.Commands.UpdateCategory;
public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    private const int NameMaxLength = 100;
    private const int DescriptionMaxLength = 500;
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId).GreaterThan(0);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(DescriptionMaxLength);
    }
}
