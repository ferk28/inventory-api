using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
namespace Inventory.Web.Authentication;
// Keycloak access tokens live five minutes, while the sign-in cookie lives much longer.
// Before each request this checks the stored token and, when it is about to expire, trades
// the refresh token for a new pair. If Keycloak refuses (the session ended), the cookie is
// rejected and the next request sends the user back to the login page.
public sealed class TokenRefreshingCookieEvents : CookieAuthenticationEvents
{
    public const string TokenClientName = "keycloak-token";
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);
    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeProvider;
    public TokenRefreshingCookieEvents(
        IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
        IHttpClientFactory httpClientFactory,
        TimeProvider timeProvider)
    {
        _oidcOptions = oidcOptions;
        _httpClientFactory = httpClientFactory;
        _timeProvider = timeProvider;
    }
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!IsAboutToExpire(context.Properties))
        {
            return;
        }
        TokenResponse? tokens = await RefreshAsync(context.Properties, context.HttpContext.RequestAborted);
        if (tokens is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }
        StoreTokens(context.Properties, tokens);
        context.ShouldRenew = true;
    }
    private bool IsAboutToExpire(AuthenticationProperties properties)
    {
        string? expiresAt = properties.GetTokenValue("expires_at");
        if (!DateTimeOffset.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset expiry))
        {
            return true;
        }

        return expiry - _timeProvider.GetUtcNow() < RefreshMargin;
    }
    private async Task<TokenResponse?> RefreshAsync(AuthenticationProperties properties, CancellationToken cancellationToken)
    {
        string? refreshToken = properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }
        OpenIdConnectOptions options = _oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
        OpenIdConnectConfiguration configuration = await options.ConfigurationManager!.GetConfigurationAsync(cancellationToken);
        FormUrlEncodedContent body = new(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = options.ClientId!,
            ["refresh_token"] = refreshToken
        });
        HttpClient client = _httpClientFactory.CreateClient(TokenClientName);
        using HttpResponseMessage response = await client.PostAsync(configuration.TokenEndpoint, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
    }
    private void StoreTokens(AuthenticationProperties properties, TokenResponse tokens)
    {
        DateTimeOffset expiresAt = _timeProvider.GetUtcNow().AddSeconds(tokens.ExpiresIn);
        properties.UpdateTokenValue("access_token", tokens.AccessToken);
        properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            properties.UpdateTokenValue("refresh_token", tokens.RefreshToken);
        }
        if (!string.IsNullOrEmpty(tokens.IdToken))
        {
            properties.UpdateTokenValue("id_token", tokens.IdToken);
        }
    }
    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("id_token")] string? IdToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
