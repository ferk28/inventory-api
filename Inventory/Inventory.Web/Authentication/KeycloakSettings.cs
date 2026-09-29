namespace Inventory.Web.Authentication;
public sealed class KeycloakSettings
{
    public const string SectionName = "Keycloak";
    public string Authority { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;
}
