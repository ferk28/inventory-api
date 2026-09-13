using System.Security.Claims;
using System.Text.Json;
namespace Inventory.Api.Authentication;
public static class KeycloakRoleFlattener
{
    private const string RealmAccessClaim = "realm_access";
    public static ClaimsPrincipal Flatten(ClaimsPrincipal principal)
    {
        Claim? realmAccess = principal.FindFirst(RealmAccessClaim);
        if (realmAccess is null)
        {
            return principal;
        }
        IEnumerable<string> roles = ReadRoles(realmAccess.Value);
        ClaimsIdentity identity = new();
        foreach (string role in roles.Where(role => !principal.IsInRole(role)))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
        principal.AddIdentity(identity);

        return principal;
    }
    private static IEnumerable<string> ReadRoles(string realmAccess)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(realmAccess);

            return ReadRoleArray(document.RootElement);
        }
        catch (JsonException)
        {
            return [];
        }
    }
    private static IEnumerable<string> ReadRoleArray(JsonElement root)
    {
        if (!root.TryGetProperty("roles", out JsonElement roles) || roles.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return roles.EnumerateArray()
            .Select(role => role.GetString())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role!)
            .ToList();
    }
}
