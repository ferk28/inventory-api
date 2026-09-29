using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
namespace Inventory.Web.Authentication;
// Attaches the signed-in user's access token to every call made through InventoryApiClient,
// so no page ever handles a token itself.
public sealed class AccessTokenHandler : DelegatingHandler
{
    private const string AccessTokenName = "access_token";
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AccessTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        string? accessToken = httpContext is null ? null : await httpContext.GetTokenAsync(AccessTokenName);
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
