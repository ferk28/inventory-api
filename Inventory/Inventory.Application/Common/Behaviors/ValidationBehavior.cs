using FluentValidation;
using FluentValidation.Results;
using MediatR;
namespace Inventory.Application.Common.Behaviors;
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ValidationFailure[] failures = await CollectFailuresAsync(request, cancellationToken);
        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
    private async Task<ValidationFailure[]> CollectFailuresAsync(TRequest request, CancellationToken cancellationToken)
    {
        ValidationContext<TRequest> context = new(request);
        ValidationResult[] results = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        return results.SelectMany(result => result.Errors).ToArray();
    }
}
