using Inventory.Web.ApiClient;
using Inventory.Web.Authentication;
using Inventory.Web.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Inventory.Web.Pages.Products;
[Authorize(Policy = InventoryRoles.WritePolicy)]
public sealed class EditModel : PageModel
{
    private readonly InventoryApiClient _apiClient;
    public EditModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty]
    public ProductForm Form { get; set; } = new();
    public int ProductId { get; private set; }
    public IReadOnlyList<SelectListItem> CategoryOptions { get; private set; } = [];
    public async Task OnGetAsync(int id, CancellationToken cancellationToken)
    {
        ProductModel product = await _apiClient.GetProductAsync(id, cancellationToken);
        ProductId = id;
        Form = new ProductForm
        {
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId
        };
        await LoadCategoriesAsync(cancellationToken);
    }
    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        ProductId = id;
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(cancellationToken);
            return Page();
        }
        try
        {
            UpdateProductRequest request = new(Form.Name.Trim(), Form.Description, Form.Price!.Value, Form.CategoryId!.Value);
            await _apiClient.UpdateProductAsync(id, request, cancellationToken);
            TempData["StatusMessage"] = $"Product {Form.Sku} updated.";

            return RedirectToPage("Details", new { id });
        }
        catch (ApiException exception) when (exception.IsUserCorrectable)
        {
            ModelState.AddApiErrors(exception, nameof(Form));
            await LoadCategoriesAsync(cancellationToken);

            return Page();
        }
    }
    // Only active categories can receive products, but the product's current category
    // stays in the list even if it was deactivated, so the form opens with it selected.
    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryModel> categories = await _apiClient.GetCategoriesAsync(true, cancellationToken);
        CategoryOptions = categories
            .Where(category => category.IsActive || category.Id == Form.CategoryId)
            .Select(category => new SelectListItem(category.IsActive ? category.Name : $"{category.Name} (inactive)", category.Id.ToString()))
            .ToList();
    }
}
