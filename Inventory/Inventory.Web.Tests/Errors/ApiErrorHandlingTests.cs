using System.Net;
using FluentAssertions;
using Inventory.Web.ApiClient;
using Inventory.Web.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
namespace Inventory.Web.Tests.Errors;
public sealed class ApiErrorHandlingTests
{
    private readonly ApiExceptionPageFilter _filter = new(NullLogger<ApiExceptionPageFilter>.Instance);
    [Fact]
    public void AddApiErrors_WithFieldErrors_PutsEachMessageUnderItsField()
    {
        ModelStateDictionary modelState = new();
        ApiException exception = new(HttpStatusCode.BadRequest, "Validation failed",
            new Dictionary<string, string[]> { ["Price"] = ["must be zero or more"] });
        modelState.AddApiErrors(exception, "Form");
        modelState["Form.Price"]!.Errors.Single().ErrorMessage.Should().Be("must be zero or more");
    }
    [Fact]
    public void AddApiErrors_WithoutFieldErrors_PutsTheMessageInTheSummary()
    {
        ModelStateDictionary modelState = new();
        modelState.AddApiErrors(new ApiException(HttpStatusCode.Conflict, "A product with SKU 'X' already exists."), "Form");
        modelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Be("A product with SKU 'X' already exists.");
    }
    [Fact]
    public void ResultFor_WithUnauthorized_SendsTheUserBackToLoginAndThenToTheSamePage()
    {
        IActionResult? result = _filter.ResultFor(new ApiException(HttpStatusCode.Unauthorized, "expired"), RequestTo("/Products", "?search=mouse"));
        result.Should().BeOfType<ChallengeResult>().Which.Properties!.RedirectUri.Should().Be("/Products?search=mouse");
    }
    [Fact]
    public void ResultFor_WithForbidden_ShowsTheAccessDeniedPage()
    {
        IActionResult? result = _filter.ResultFor(new ApiException(HttpStatusCode.Forbidden, "no"), RequestTo("/Products/Create"));
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/AccessDenied");
    }
    [Fact]
    public void ResultFor_WithNotFound_ShowsTheNotFoundPage()
    {
        IActionResult? result = _filter.ResultFor(new ApiException(HttpStatusCode.NotFound, "missing"), RequestTo("/Products/Details/9"));
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/NotFound");
    }
    [Fact]
    public void ResultFor_WhenTheApiCannotBeReached_ShowsTheUnavailablePageWithARetryLink()
    {
        IActionResult? result = _filter.ResultFor(new HttpRequestException("connection refused"), RequestTo("/Movements", "?type=Out"));
        RedirectToPageResult redirect = result.Should().BeOfType<RedirectToPageResult>().Which;
        redirect.PageName.Should().Be("/ApiUnavailable");
        redirect.RouteValues!["returnUrl"].Should().Be("/Movements?type=Out");
    }
    [Fact]
    public void ResultFor_WithAnErrorTheFormShows_LeavesItToThePage()
    {
        IActionResult? result = _filter.ResultFor(new ApiException(HttpStatusCode.Conflict, "duplicate"), RequestTo("/Products/Create"));
        result.Should().BeNull();
    }
    private static HttpContext RequestTo(string path, string query = "")
    {
        DefaultHttpContext context = new();
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);

        return context;
    }
}
