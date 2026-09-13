using System.Security.Claims;
using FluentAssertions;
using Inventory.Api.Authentication;
using Microsoft.Extensions.Configuration;
namespace Inventory.UnitTests.Api;
public sealed class KeycloakAuthenticationTests
{
    [Fact]
    public void FromEnvironment_WithEveryVariable_ReadsTheSettings()
    {
        IConfiguration configuration = Configuration(new Dictionary<string, string?>
        {
            ["KEYCLOAK_AUTHORITY"] = "http://keycloak:8080/realms/inventory",
            ["KEYCLOAK_AUDIENCE"] = "inventory-api"
        });
        AuthenticationSettings settings = AuthenticationSettingsReader.FromEnvironment(configuration);
        settings.Authority.Should().Be("http://keycloak:8080/realms/inventory");
        settings.Audience.Should().Be("inventory-api");
    }
    [Fact]
    public void FromEnvironment_WithoutAuthority_ExplainsWhichVariableIsMissing()
    {
        IConfiguration configuration = Configuration(new Dictionary<string, string?>
        {
            ["KEYCLOAK_AUDIENCE"] = "inventory-api"
        });
        Action read = () => AuthenticationSettingsReader.FromEnvironment(configuration);
        read.Should().Throw<InvalidOperationException>().WithMessage("*KEYCLOAK_AUTHORITY*");
    }
    [Fact]
    public void FromEnvironment_ByDefault_RequiresHttpsMetadata()
    {
        IConfiguration configuration = Configuration(new Dictionary<string, string?>
        {
            ["KEYCLOAK_AUTHORITY"] = "https://keycloak/realms/inventory",
            ["KEYCLOAK_AUDIENCE"] = "inventory-api"
        });
        AuthenticationSettings settings = AuthenticationSettingsReader.FromEnvironment(configuration);
        settings.RequireHttpsMetadata.Should().BeTrue();
    }
    [Fact]
    public void FromEnvironment_WhenHttpsMetadataIsDisabled_ReadsTheOptOut()
    {
        IConfiguration configuration = Configuration(new Dictionary<string, string?>
        {
            ["KEYCLOAK_AUTHORITY"] = "http://keycloak:8080/realms/inventory",
            ["KEYCLOAK_AUDIENCE"] = "inventory-api",
            ["KEYCLOAK_REQUIRE_HTTPS_METADATA"] = "false"
        });
        AuthenticationSettings settings = AuthenticationSettingsReader.FromEnvironment(configuration);
        settings.RequireHttpsMetadata.Should().BeFalse();
    }
    [Fact]
    public void Flatten_WithRealmRoles_TurnsThemIntoRoleClaims()
    {
        ClaimsPrincipal principal = PrincipalWithRealmAccess("""{"roles":["inventory.read","inventory.write"]}""");
        ClaimsPrincipal flattened = KeycloakRoleFlattener.Flatten(principal);
        flattened.IsInRole("inventory.read").Should().BeTrue();
        flattened.IsInRole("inventory.write").Should().BeTrue();
    }
    [Fact]
    public void Flatten_WithoutRealmAccess_LeavesThePrincipalAlone()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "user-1")], "jwt"));
        ClaimsPrincipal flattened = KeycloakRoleFlattener.Flatten(principal);
        flattened.Claims.Should().ContainSingle();
    }
    [Fact]
    public void Flatten_WithMalformedRealmAccess_LeavesThePrincipalAlone()
    {
        ClaimsPrincipal principal = PrincipalWithRealmAccess("not json at all");
        ClaimsPrincipal flattened = KeycloakRoleFlattener.Flatten(principal);
        flattened.IsInRole("inventory.read").Should().BeFalse();
    }
    [Fact]
    public void Flatten_RunTwice_DoesNotDuplicateTheRoles()
    {
        ClaimsPrincipal principal = PrincipalWithRealmAccess("""{"roles":["inventory.read"]}""");
        ClaimsPrincipal flattened = KeycloakRoleFlattener.Flatten(KeycloakRoleFlattener.Flatten(principal));
        flattened.FindAll(ClaimTypes.Role).Should().ContainSingle();
    }
    private static ClaimsPrincipal PrincipalWithRealmAccess(string realmAccess)
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim("realm_access", realmAccess)], "jwt"));
    }
    private static IConfiguration Configuration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
