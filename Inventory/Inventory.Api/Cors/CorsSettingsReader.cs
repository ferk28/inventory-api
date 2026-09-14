namespace Inventory.Api.Cors;
public static class CorsSettingsReader
{
    private const char OriginSeparator = ',';
    public static CorsSettings FromEnvironment(IConfiguration configuration)
    {
        string? origins = configuration["CORS_ALLOWED_ORIGINS"];

        return new CorsSettings { AllowedOrigins = SplitOrigins(origins) };
    }
    private static string[] SplitOrigins(string? origins)
    {
        if (string.IsNullOrWhiteSpace(origins))
        {
            return [];
        }

        return origins.Split(OriginSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
