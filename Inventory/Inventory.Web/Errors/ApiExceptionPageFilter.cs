using System.Net;
using Inventory.Web.ApiClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace Inventory.Web.Errors;
// Pages only catch the API errors a user can fix in the form they sent (400, 409, 422).
// Everything else lands here and becomes a page that says what happened, instead of a
// stack trace: an expired session, a missing permission, a record that no longer exists,
// or an API that is not running.
public sealed class ApiExceptionPageFilter : IAsyncPageFilter
{
    private readonly ILogger<ApiExceptionPageFilter> _logger;
    public ApiExceptionPageFilter(ILogger<ApiExceptionPageFilter> logger)
    {
        _logger = logger;
    }
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        PageHandlerExecutedContext executed = await next();
        if (executed.Exception is null || executed.ExceptionHandled)
        {
            return;
        }
        IActionResult? result = ResultFor(executed.Exception, context.HttpContext);
        if (result is null)
        {
            return;
        }
        executed.Result = result;
        executed.ExceptionHandled = true;
    }
    internal IActionResult? ResultFor(Exception exception, HttpContext httpContext)
    {
        string returnUrl = $"{httpContext.Request.PathBase}{httpContext.Request.Path}{httpContext.Request.QueryString}";
        if (exception is ApiException apiException)
        {
            return apiException.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new ChallengeResult(new AuthenticationProperties { RedirectUri = returnUrl }),
                HttpStatusCode.Forbidden => new RedirectToPageResult("/AccessDenied"),
                HttpStatusCode.NotFound => new RedirectToPageResult("/NotFound"),
                _ => null
            };
        }
        if (IsApiUnreachable(exception, httpContext))
        {
            _logger.LogWarning(exception, "The inventory API could not be reached.");

            return new RedirectToPageResult("/ApiUnavailable", new { returnUrl });
        }

        return null;
    }
    // A timeout surfaces as TaskCanceledException too; only a cancelled browser request
    // must not be reported as an unreachable API.
    private static bool IsApiUnreachable(Exception exception, HttpContext httpContext)
    {
        return exception is HttpRequestException
            || (exception is TaskCanceledException && !httpContext.RequestAborted.IsCancellationRequested);
    }
}
