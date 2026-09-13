using FluentValidation;
namespace Inventory.Application.Products.Queries.GetProducts;
public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    private const int MaxPageSize = 100;
    public GetProductsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).GreaterThan(0).LessThanOrEqualTo(MaxPageSize);
    }
}
