namespace Inventory.Api.Authentication;
public static class AuthenticationSettingsReader
{
    public static AuthenticationSettings FromEnvironment(IConfiguration configuration)
    {
        return new AuthenticationSettings
        {
            Authority = RequireValue(configuration, "KEYCLOAK_AUTHORITY"),
            Audience = RequireValue(configuration, "KEYCLOAK_AUDIENCE"),
            RequireHttpsMetadata = ReadFlag(configuration, "KEYCLOAK_REQUIRE_HTTPS_METADATA")
        };
    }
    private static string RequireValue(IConfiguration configuration, string key)
    {
        string? value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is required but was not set.");
        }

        return value;
    }
    private static bool ReadFlag(IConfiguration configuration, string key)
    {
        string? value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || bool.Parse(value);
    }
}
