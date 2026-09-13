using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
namespace Inventory.Api.Authentication;
public static class AuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        AuthenticationSettings settings = AuthenticationSettingsReader.FromEnvironment(configuration);
        services.AddSingleton(settings);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => ConfigureJwtBearer(options, settings));
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Read, policy => policy.RequireRole(AuthorizationPolicies.Read))
            .AddPolicy(AuthorizationPolicies.Write, policy => policy.RequireRole(AuthorizationPolicies.Write));

        return services;
    }
    private static void ConfigureJwtBearer(JwtBearerOptions options, AuthenticationSettings settings)
    {
        options.Authority = settings.Authority;
        options.MetadataAddress = settings.MetadataAddress;
        options.Audience = settings.Audience;
        options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Authority,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context => FlattenRoles(context)
        };
    }
    private static Task FlattenRoles(TokenValidatedContext context)
    {
        if (context.Principal is not null)
        {
            context.Principal = KeycloakRoleFlattener.Flatten(context.Principal);
        }

        return Task.CompletedTask;
    }
}
