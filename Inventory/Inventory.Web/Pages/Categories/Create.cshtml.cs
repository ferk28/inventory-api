using Inventory.Web.ApiClient;
using Inventory.Web.Authentication;
using Inventory.Web.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages.Categories;
[Authorize(Policy = InventoryRoles.WritePolicy)]
public sealed class CreateModel : PageModel
{
    private readonly InventoryApiClient _apiClient;
    public CreateModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty]
    public CategoryForm Form { get; set; } = new();
    public void OnGet()
    {
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        try
        {
            CategoryModel category = await _apiClient.CreateCategoryAsync(new CategoryRequest(Form.Name.Trim(), Form.Description), cancellationToken);
            TempData["StatusMessage"] = $"Category {category.Name} created.";

            return RedirectToPage("Index");
        }
        catch (ApiException exception) when (exception.IsUserCorrectable)
        {
            ModelState.AddApiErrors(exception, nameof(Form));

            return Page();
        }
    }
}
