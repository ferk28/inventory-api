using System.Diagnostics;
using System.Globalization;
using Inventory.Web.ApiClient;
using Inventory.Web.Authentication;
using Inventory.Web.Errors;
// ContentRootPath is the executable's folder, so wwwroot and appsettings.json are found
// even when the published .exe is started from a shortcut or another working directory.
// The UI text and the API are English, and a fixed culture keeps "19.99" parsing as a price
// on machines whose regional settings use a decimal comma.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
InventoryApiSettings apiSettings = builder.Configuration.GetSection(InventoryApiSettings.SectionName).Get<InventoryApiSettings>()
    ?? throw new InvalidOperationException("InventoryApi:BaseUrl must be configured in appsettings.json.");
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddTransient<AccessTokenHandler>();
builder.Services.AddHttpClient<InventoryApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiSettings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);
    })
    .AddHttpMessageHandler<AccessTokenHandler>();
builder.Services.AddKeycloakLogin(builder.Configuration);
builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AllowAnonymousToPage("/Error");
        options.Conventions.AllowAnonymousToPage("/ApiUnavailable");
    })
    .AddMvcOptions(options => options.Filters.Add<ApiExceptionPageFilter>());
WebApplication app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// CSS and scripts are public: the error pages are shown to signed-out users too.
app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages().WithStaticAssets();
if (builder.Configuration.GetValue<bool>("OpenBrowserOnStart"))
{
    app.Lifetime.ApplicationStarted.Register(() => OpenBrowser(app.Urls.First()));
}
app.Run();
static void OpenBrowser(string url)
{
    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
