using Inventory.Web.ApiClient;
using Inventory.Web.Authentication;
using Inventory.Web.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages.Categories;
[Authorize(Policy = InventoryRoles.WritePolicy)]
public sealed class EditModel : PageModel
{
    private readonly InventoryApiClient _apiClient;
    public EditModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty]
    public CategoryForm Form { get; set; } = new();
    public string OriginalName { get; private set; } = string.Empty;
    public async Task OnGetAsync(int id, CancellationToken cancellationToken)
    {
        CategoryModel category = await _apiClient.GetCategoryAsync(id, cancellationToken);
        OriginalName = category.Name;
        Form = new CategoryForm { Name = category.Name, Description = category.Description };
    }
    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        OriginalName = Form.Name;
        if (!ModelState.IsValid)
        {
            return Page();
        }
        try
        {
            await _apiClient.UpdateCategoryAsync(id, new CategoryRequest(Form.Name.Trim(), Form.Description), cancellationToken);
            TempData["StatusMessage"] = $"Category {Form.Name.Trim()} updated.";

            return RedirectToPage("Index");
        }
        catch (ApiException exception) when (exception.IsUserCorrectable)
        {
            ModelState.AddApiErrors(exception, nameof(Form));

            return Page();
        }
    }
}
