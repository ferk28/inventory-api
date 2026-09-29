using Inventory.Web.ApiClient;
using Inventory.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages.Products;
[Authorize(Policy = InventoryRoles.WritePolicy)]
public sealed class DeleteModel : PageModel
{
    private readonly InventoryApiClient _apiClient;
    public DeleteModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    public ProductModel Product { get; private set; } = null!;
    public string? ErrorMessage { get; private set; }
    public async Task OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Product = await _apiClient.GetProductAsync(id, cancellationToken);
    }
    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteProductAsync(id, cancellationToken);
            TempData["StatusMessage"] = "Product deleted. Its movement history is kept.";

            return RedirectToPage("Index");
        }
        catch (ApiException exception) when (exception.IsUserCorrectable)
        {
            ErrorMessage = exception.Message;
            Product = await _apiClient.GetProductAsync(id, cancellationToken);

            return Page();
        }
    }
}
