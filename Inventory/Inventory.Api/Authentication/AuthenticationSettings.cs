namespace Inventory.Api.Authentication;
public sealed class AuthenticationSettings
{
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;
}
