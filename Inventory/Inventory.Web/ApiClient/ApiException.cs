using System.Net;
namespace Inventory.Web.ApiClient;
// Every non-success answer of the API becomes this exception, carrying what its
// ProblemDetails body said, so pages decide what to show from the status code alone.
public sealed class ApiException : Exception
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors = new Dictionary<string, string[]>();
    public ApiException(HttpStatusCode statusCode, string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? NoErrors;
    }
    public HttpStatusCode StatusCode { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    // 400, 409 and 422 describe something the user can fix in the form they just sent.
    public bool IsUserCorrectable => StatusCode is HttpStatusCode.BadRequest
        or HttpStatusCode.Conflict
        or HttpStatusCode.UnprocessableEntity;
}
