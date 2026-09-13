using Inventory.Api.Authentication;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;
namespace Inventory.Api.Swagger;
public static class SwaggerExtensions
{
    private const string SecuritySchemeId = "oauth2";
    public static IServiceCollection AddInventorySwagger(this IServiceCollection services, IConfiguration configuration)
    {
        AuthenticationSettings settings = AuthenticationSettingsReader.FromEnvironment(configuration);
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Inventory API",
                Version = "v1",
                Description = "Product inventory: categories, products and stock movements."
            });
            options.AddSecurityDefinition(SecuritySchemeId, BuildSecurityScheme(settings));
            options.AddSecurityRequirement(document => BuildSecurityRequirement(document));
        });

        return services;
    }
    public static void ConfigureInventoryUi(this SwaggerUIOptions options, IConfiguration configuration)
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
        options.OAuthClientId(configuration["KEYCLOAK_SWAGGER_CLIENT_ID"] ?? "inventory-swagger");
        options.OAuthScopes("openid", "profile");
        options.OAuthUsePkce();
    }
    private static OpenApiSecurityScheme BuildSecurityScheme(AuthenticationSettings settings)
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Keycloak realm 'inventory'. Use the test user admin / admin.",
            Flows = new OpenApiOAuthFlows
            {
                Password = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri($"{settings.Authority}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "OpenID Connect scope",
                        ["profile"] = "User profile"
                    }
                }
            }
        };
    }
    private static OpenApiSecurityRequirement BuildSecurityRequirement(OpenApiDocument document)
    {
        OpenApiSecuritySchemeReference reference = new(SecuritySchemeId, document);

        return new OpenApiSecurityRequirement { [reference] = ["openid", "profile"] };
    }
}
