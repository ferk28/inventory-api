using System.Globalization;
using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Inventory.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
namespace Inventory.Web.Tests.Authentication;
public sealed class TokenRefreshingCookieEventsTests
{
    private const string TokenEndpoint = "http://keycloak.test/token";
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    [Fact]
    public async Task ValidatePrincipal_WithTokenStillValid_DoesNotCallKeycloak()
    {
        StubHttpMessageHandler keycloak = new(HttpStatusCode.OK, RefreshedTokens());
        CookieValidatePrincipalContext context = CreateContext(expiresAt: Now.AddMinutes(4));
        await CreateEvents(keycloak).ValidatePrincipal(context);
        keycloak.Requests.Should().BeEmpty();
        context.ShouldRenew.Should().BeFalse();
    }
    [Fact]
    public async Task ValidatePrincipal_WithTokenAboutToExpire_StoresTheRefreshedTokens()
    {
        StubHttpMessageHandler keycloak = new(HttpStatusCode.OK, RefreshedTokens());
        CookieValidatePrincipalContext context = CreateContext(expiresAt: Now.AddSeconds(30));
        await CreateEvents(keycloak).ValidatePrincipal(context);
        context.Properties.GetTokenValue("access_token").Should().Be("new-access");
        context.Properties.GetTokenValue("refresh_token").Should().Be("new-refresh");
        context.Properties.GetTokenValue("expires_at").Should().Be(Now.AddMinutes(5).ToString("o", CultureInfo.InvariantCulture));
        context.ShouldRenew.Should().BeTrue();
    }
    [Fact]
    public async Task ValidatePrincipal_WithTokenAboutToExpire_SendsARefreshGrantForThisClient()
    {
        StubHttpMessageHandler keycloak = new(HttpStatusCode.OK, RefreshedTokens());
        await CreateEvents(keycloak).ValidatePrincipal(CreateContext(expiresAt: Now.AddSeconds(30)));
        keycloak.Requests.Single().RequestUri.Should().Be(new Uri(TokenEndpoint));
        keycloak.RequestBodies.Single().Should().Be("grant_type=refresh_token&client_id=inventory-web&refresh_token=old-refresh");
    }
    [Fact]
    public async Task ValidatePrincipal_WhenKeycloakRefusesTheRefresh_SignsTheUserOut()
    {
        StubHttpMessageHandler keycloak = new(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");
        CookieValidatePrincipalContext context = CreateContext(expiresAt: Now.AddSeconds(30));
        await CreateEvents(keycloak).ValidatePrincipal(context);
        context.Principal.Should().BeNull();
        await _authenticationService.Received(1).SignOutAsync(
            Arg.Any<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, Arg.Any<AuthenticationProperties?>());
    }
    [Fact]
    public async Task ValidatePrincipal_WithoutARefreshToken_SignsTheUserOut()
    {
        StubHttpMessageHandler keycloak = new(HttpStatusCode.OK, RefreshedTokens());
        CookieValidatePrincipalContext context = CreateContext(expiresAt: Now.AddSeconds(30), refreshToken: null);
        await CreateEvents(keycloak).ValidatePrincipal(context);
        context.Principal.Should().BeNull();
        keycloak.Requests.Should().BeEmpty();
    }
    private static TokenRefreshingCookieEvents CreateEvents(StubHttpMessageHandler keycloak)
    {
        OpenIdConnectOptions options = new()
        {
            ClientId = "inventory-web",
            ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                new OpenIdConnectConfiguration { TokenEndpoint = TokenEndpoint })
        };
        IOptionsMonitor<OpenIdConnectOptions> monitor = Substitute.For<IOptionsMonitor<OpenIdConnectOptions>>();
        monitor.Get(OpenIdConnectDefaults.AuthenticationScheme).Returns(options);
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(TokenRefreshingCookieEvents.TokenClientName).Returns(new HttpClient(keycloak));

        return new TokenRefreshingCookieEvents(monitor, factory, new FixedTimeProvider(Now));
    }
    private CookieValidatePrincipalContext CreateContext(DateTimeOffset expiresAt, string? refreshToken = "old-refresh")
    {
        ServiceCollection services = new();
        services.AddSingleton(_authenticationService);
        DefaultHttpContext httpContext = new() { RequestServices = services.BuildServiceProvider() };
        AuthenticationProperties properties = new();
        List<AuthenticationToken> tokens =
        [
            new() { Name = "access_token", Value = "old-access" },
            new() { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) }
        ];
        if (refreshToken is not null)
        {
            tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
        }
        properties.StoreTokens(tokens);
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("preferred_username", "admin")], "test"));
        AuthenticationScheme scheme = new(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        AuthenticationTicket ticket = new(principal, properties, scheme.Name);

        return new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket);
    }
    private static string RefreshedTokens()
    {
        return """{"access_token":"new-access","refresh_token":"new-refresh","id_token":"new-id","expires_in":300}""";
    }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
