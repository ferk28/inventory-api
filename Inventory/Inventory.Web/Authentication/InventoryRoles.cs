using System.Security.Claims;
namespace Inventory.Web.Authentication;
public static class InventoryRoles
{
    public const string Write = "inventory.write";
    public const string WritePolicy = "InventoryWrite";
    public static bool CanWrite(this ClaimsPrincipal user)
    {
        return user.IsInRole(Write);
    }
}
