using Inventory.Web.ApiClient;
using Inventory.Web.Authentication;
using Inventory.Web.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Inventory.Web.Pages.Products;
[Authorize(Policy = InventoryRoles.WritePolicy)]
public sealed class CreateModel : PageModel
{
    private readonly InventoryApiClient _apiClient;
    public CreateModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty]
    public ProductForm Form { get; set; } = new();
    public IReadOnlyList<SelectListItem> CategoryOptions { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadCategoriesAsync(cancellationToken);
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(cancellationToken);
            return Page();
        }
        try
        {
            CreateProductRequest request = new(Form.Sku.Trim(), Form.Name.Trim(), Form.Description, Form.Price!.Value, Form.CategoryId!.Value);
            ProductModel product = await _apiClient.CreateProductAsync(request, cancellationToken);
            TempData["StatusMessage"] = $"Product {product.Sku} created. It starts with no stock: register an entry to add units.";

            return RedirectToPage("Details", new { id = product.Id });
        }
        catch (ApiException exception) when (exception.IsUserCorrectable)
        {
            ModelState.AddApiErrors(exception, nameof(Form));
            await LoadCategoriesAsync(cancellationToken);

            return Page();
        }
    }
    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryModel> categories = await _apiClient.GetCategoriesAsync(false, cancellationToken);
        CategoryOptions = categories.Select(category => new SelectListItem(category.Name, category.Id.ToString())).ToList();
    }
}
