using Inventory.Web.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages;
public sealed class ApiUnavailableModel : PageModel
{
    private readonly IConfiguration _configuration;
    public ApiUnavailableModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }
    public string ApiBaseUrl { get; private set; } = string.Empty;
    public string RetryUrl { get; private set; } = "/";
    public void OnGet()
    {
        ApiBaseUrl = _configuration[$"{InventoryApiSettings.SectionName}:BaseUrl"] ?? string.Empty;
        RetryUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/";
    }
}
