using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
namespace Inventory.Web.Authentication;
public static class KeycloakLoginExtensions
{
    private const string RolesClaim = "roles";
    private const string UserNameClaim = "preferred_username";
    public static IServiceCollection AddKeycloakLogin(this IServiceCollection services, IConfiguration configuration)
    {
        KeycloakSettings settings = ReadSettings(configuration);
        services.AddHttpClient(TokenRefreshingCookieEvents.TokenClientName);
        services.AddScoped<TokenRefreshingCookieEvents>();
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "inventory-web";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.AccessDeniedPath = "/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.EventsType = typeof(TokenRefreshingCookieEvents);
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = settings.Authority;
                options.ClientId = settings.ClientId;
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                // Authorization code with PKCE: the app is a public client, so no secret
                // ships inside the executable.
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.TokenValidationParameters.NameClaimType = UserNameClaim;
                options.TokenValidationParameters.RoleClaimType = RolesClaim;
                // Keycloak and this app both run on localhost, often over plain HTTP, so the
                // login round trip is same-site and Lax cookies are enough.
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(InventoryRoles.WritePolicy, policy => policy.RequireRole(InventoryRoles.Write));
        });

        return services;
    }
    private static KeycloakSettings ReadSettings(IConfiguration configuration)
    {
        KeycloakSettings? settings = configuration.GetSection(KeycloakSettings.SectionName).Get<KeycloakSettings>();
        if (settings is null || string.IsNullOrWhiteSpace(settings.Authority) || string.IsNullOrWhiteSpace(settings.ClientId))
        {
            throw new InvalidOperationException("Keycloak:Authority and Keycloak:ClientId must be configured in appsettings.json.");
        }

        return settings;
    }
}
