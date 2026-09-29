using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Inventory.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
namespace Inventory.Web.Tests.Authentication;
public sealed class AccessTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_WithSignedInUser_SendsTheirAccessTokenAsBearer()
    {
        StubHttpMessageHandler api = new(HttpStatusCode.OK, "{}");
        HttpClient client = CreateClient(SignedInContext("the-access-token"), api);
        await client.GetAsync("api/products");
        api.Requests.Single().Headers.Authorization!.ToString().Should().Be("Bearer the-access-token");
    }
    [Fact]
    public async Task SendAsync_WithoutHttpContext_SendsNoAuthorizationHeader()
    {
        StubHttpMessageHandler api = new(HttpStatusCode.OK, "{}");
        HttpClient client = CreateClient(null, api);
        await client.GetAsync("api/products");
        api.Requests.Single().Headers.Authorization.Should().BeNull();
    }
    private static HttpClient CreateClient(HttpContext? httpContext, StubHttpMessageHandler api)
    {
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        AccessTokenHandler handler = new(accessor) { InnerHandler = api };

        return new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
    }
    private static HttpContext SignedInContext(string accessToken)
    {
        AuthenticationProperties properties = new();
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = accessToken }]);
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("preferred_username", "admin")], "test"));
        AuthenticateResult result = AuthenticateResult.Success(new AuthenticationTicket(principal, properties, "Cookies"));
        IAuthenticationService authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string?>()).Returns(result);
        ServiceCollection services = new();
        services.AddSingleton(authentication);

        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
    }
}
