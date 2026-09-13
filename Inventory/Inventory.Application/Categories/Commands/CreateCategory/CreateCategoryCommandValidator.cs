using FluentValidation;
namespace Inventory.Application.Categories.Commands.CreateCategory;
public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    private const int NameMaxLength = 100;
    private const int DescriptionMaxLength = 500;
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(DescriptionMaxLength);
    }
}
