using Inventory.Api.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace Inventory.Api.Errors;
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;
    public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _logger = logger;
    }
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = ProblemDetailsMapper.Map(exception);
        LogWhenUnexpected(problem, exception);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        problem.Instance = httpContext.Request.Path;
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), cancellationToken);

        return true;
    }
    private void LogWhenUnexpected(ProblemDetails problem, Exception exception)
    {
        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing the request.");
        }
    }
}
