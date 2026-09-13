namespace Inventory.Api.Authentication;
public static class AuthenticationSettingsReader
{
    private const string DiscoveryPath = "/.well-known/openid-configuration";
    public static AuthenticationSettings FromEnvironment(IConfiguration configuration)
    {
        string authority = RequireValue(configuration, "KEYCLOAK_AUTHORITY");

        return new AuthenticationSettings
        {
            Authority = authority,
            Audience = RequireValue(configuration, "KEYCLOAK_AUDIENCE"),
            MetadataAddress = ReadMetadataAddress(configuration, authority),
            RequireHttpsMetadata = ReadFlag(configuration, "KEYCLOAK_REQUIRE_HTTPS_METADATA")
        };
    }
    private static string ReadMetadataAddress(IConfiguration configuration, string authority)
    {
        string? explicitAddress = configuration["KEYCLOAK_METADATA_ADDRESS"];

        return string.IsNullOrWhiteSpace(explicitAddress) ? $"{authority}{DiscoveryPath}" : explicitAddress;
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
