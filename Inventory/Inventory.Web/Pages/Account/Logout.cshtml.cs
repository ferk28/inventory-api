using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages.Account;
public sealed class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }
    // Signing out of the OIDC scheme also ends the Keycloak session, so the next visit asks
    // for credentials again instead of silently signing the same user back in.
    public IActionResult OnPost()
    {
        AuthenticationProperties properties = new() { RedirectUri = Url.Page("/Index") };

        return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }
}
