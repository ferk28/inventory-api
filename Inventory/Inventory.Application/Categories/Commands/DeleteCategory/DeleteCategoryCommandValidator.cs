using FluentValidation;
namespace Inventory.Application.Categories.Commands.DeleteCategory;
public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId).GreaterThan(0);
    }
}
