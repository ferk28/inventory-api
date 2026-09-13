using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Inventory.Api.Errors;
using Inventory.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
namespace Inventory.UnitTests.Api;
public sealed class ProblemDetailsExceptionHandlerTests
{
    private readonly ProblemDetailsExceptionHandler _handler = new(NullLogger<ProblemDetailsExceptionHandler>.Instance);
    [Fact]
    public async Task TryHandleAsync_WithValidationException_WritesTheFieldErrorsIntoTheBody()
    {
        ValidationException exception = new([new ValidationFailure("Quantity", "must be greater than 0")]);
        string body = await HandleAndReadBodyAsync(exception);
        JsonElement payload = JsonDocument.Parse(body).RootElement;
        payload.TryGetProperty("errors", out JsonElement errors).Should().BeTrue(
            "SPEC section 7.1 requires the 400 envelope to carry field to messages");
        errors.GetProperty("Quantity").EnumerateArray().Should().ContainSingle();
    }
    [Fact]
    public async Task TryHandleAsync_WithValidationException_SetsStatus400()
    {
        ValidationException exception = new([new ValidationFailure("Quantity", "must be greater than 0")]);
        DefaultHttpContext context = CreateContext();
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
    [Fact]
    public async Task TryHandleAsync_WithNotFoundException_WritesTheProblemDetailBody()
    {
        string body = await HandleAndReadBodyAsync(new NotFoundException("Product", 12));
        JsonElement payload = JsonDocument.Parse(body).RootElement;
        payload.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        payload.GetProperty("detail").GetString().Should().Contain("12");
    }
    [Fact]
    public async Task TryHandleAsync_RecordsTheRequestPathAsInstance()
    {
        DefaultHttpContext context = CreateContext();
        context.Request.Path = "/api/products/12";
        await _handler.TryHandleAsync(context, new NotFoundException("Product", 12), CancellationToken.None);
        string body = await ReadBodyAsync(context);
        JsonDocument.Parse(body).RootElement.GetProperty("instance").GetString().Should().Be("/api/products/12");
    }
    private async Task<string> HandleAndReadBodyAsync(Exception exception)
    {
        DefaultHttpContext context = CreateContext();
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        return await ReadBodyAsync(context);
    }
    private static DefaultHttpContext CreateContext()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        return context;
    }
    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body);

        return await reader.ReadToEndAsync();
    }
}
