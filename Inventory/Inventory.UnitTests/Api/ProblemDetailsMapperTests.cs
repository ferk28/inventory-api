using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Inventory.Api.Errors;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
namespace Inventory.UnitTests.Api;
public sealed class ProblemDetailsMapperTests
{
    [Fact]
    public void Map_WithValidationException_Returns400()
    {
        ValidationException exception = new([new ValidationFailure("Quantity", "must be greater than 0")]);
        ProblemDetails problem = ProblemDetailsMapper.Map(exception);
        problem.Status.Should().Be(400);
    }
    [Fact]
    public void Map_WithValidationException_ListsEveryFieldError()
    {
        ValidationException exception = new(
        [
            new ValidationFailure("Quantity", "must be greater than 0"),
            new ValidationFailure("ProductId", "must be greater than 0")
        ]);
        ValidationProblemDetails problem = ProblemDetailsMapper.Map(exception).Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKeys("Quantity", "ProductId");
    }
    [Fact]
    public void Map_WithNotFoundException_Returns404()
    {
        ProblemDetails problem = ProblemDetailsMapper.Map(new NotFoundException("Product", 12));
        problem.Status.Should().Be(404);
    }
    [Fact]
    public void Map_WithConflictException_Returns409()
    {
        ProblemDetails problem = ProblemDetailsMapper.Map(new ConflictException("A product with SKU 'X' already exists."));
        problem.Status.Should().Be(409);
    }
    [Fact]
    public void Map_WithInsufficientStock_Returns422()
    {
        ProblemDetails problem = ProblemDetailsMapper.Map(new InsufficientStockException(12, 3, 5));
        problem.Status.Should().Be(422);
    }
    [Fact]
    public void Map_WithInsufficientStock_KeepsTheDomainMessageAsDetail()
    {
        ProblemDetails problem = ProblemDetailsMapper.Map(new InsufficientStockException(12, 3, 5));
        problem.Detail.Should().Contain("12").And.Contain("3").And.Contain("5");
    }
    [Fact]
    public void Map_WithProductInactive_Returns422()
    {
        ProblemDetails problem = ProblemDetailsMapper.Map(new ProductInactiveException(12));
        problem.Status.Should().Be(422);
    }
    [Fact]
    public void Map_WithUnexpectedException_Returns500()
    {
        ProblemDetails problem = ProblemDetailsMapper.Map(new InvalidOperationException("connection reset by peer"));
        problem.Status.Should().Be(500);
    }
    [Fact]
    public void Map_WithUnexpectedException_HidesTheOriginalMessage()
    {
        ProblemDetails problem = ProblemDetailsMapper.Map(new InvalidOperationException("connection reset by peer"));
        problem.Detail.Should().NotContain("connection reset by peer");
    }
}
