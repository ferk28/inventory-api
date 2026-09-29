using Inventory.Web.ApiClient;
using Inventory.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages.Categories;
[Authorize(Policy = InventoryRoles.WritePolicy)]
public sealed class DeleteModel : PageModel
{
    private readonly InventoryApiClient _apiClient;
    public DeleteModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    public CategoryModel Category { get; private set; } = null!;
    public string? ErrorMessage { get; private set; }
    public async Task OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Category = await _apiClient.GetCategoryAsync(id, cancellationToken);
    }
    // The API refuses (409) to delete a category that still has active products; that
    // message is shown here, next to a link to exactly those products.
    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _apiClient.DeleteCategoryAsync(id, cancellationToken);
            TempData["StatusMessage"] = "Category deleted.";

            return RedirectToPage("Index");
        }
        catch (ApiException exception) when (exception.IsUserCorrectable)
        {
            ErrorMessage = exception.Message;
            Category = await _apiClient.GetCategoryAsync(id, cancellationToken);

            return Page();
        }
    }
}
