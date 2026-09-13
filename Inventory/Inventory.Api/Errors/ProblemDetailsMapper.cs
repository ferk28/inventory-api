using FluentValidation;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
namespace Inventory.Api.Errors;
public static class ProblemDetailsMapper
{
    private const string UnexpectedErrorDetail = "An unexpected error occurred while processing the request.";
    public static ProblemDetails Map(Exception exception)
    {
        return exception switch
        {
            ValidationException validation => FromValidation(validation),
            NotFoundException notFound => Build(StatusCodes.Status404NotFound, "Resource not found", notFound.Message),
            ConflictException conflict => Build(StatusCodes.Status409Conflict, "Conflict", conflict.Message),
            DomainException domain => Build(StatusCodes.Status422UnprocessableEntity, "Business rule violation", domain.Message),
            _ => Build(StatusCodes.Status500InternalServerError, "Unexpected error", UnexpectedErrorDetail)
        };
    }
    private static ValidationProblemDetails FromValidation(ValidationException exception)
    {
        Dictionary<string, string[]> errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());
        ValidationProblemDetails problem = new(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Type = TypeFor(StatusCodes.Status400BadRequest)
        };

        return problem;
    }
    private static ProblemDetails Build(int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = TypeFor(status)
        };
    }
    private static string TypeFor(int status)
    {
        return $"https://httpstatuses.com/{status}";
    }
}
