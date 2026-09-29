using Inventory.Web.ApiClient;
using Inventory.Web.Authentication;
using Inventory.Web.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Inventory.Web.Pages.Movements;
[Authorize(Policy = InventoryRoles.WritePolicy)]
public sealed class CreateModel : PageModel
{
    private const int ProductOptionLimit = 100;
    private readonly InventoryApiClient _apiClient;
    public CreateModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty]
    public MovementForm Form { get; set; } = new();
    public IReadOnlyList<SelectListItem> ProductOptions { get; private set; } = [];
    public async Task OnGetAsync(int? productId, CancellationToken cancellationToken)
    {
        Form = new MovementForm { ProductId = productId, Type = MovementType.In };
        await LoadProductsAsync(cancellationToken);
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadProductsAsync(cancellationToken);
            return Page();
        }
        try
        {
            RegisterMovementRequest request = new(Form.ProductId!.Value, Form.Type!.Value, Form.Quantity!.Value, Form.Reason);
            MovementModel movement = await _apiClient.RegisterMovementAsync(request, cancellationToken);
            string direction = movement.Type == MovementType.In ? "added to" : "taken from";
            TempData["StatusMessage"] = $"{movement.Quantity} units {direction} {movement.ProductSku}. Stock is now {movement.StockAfter}.";

            return RedirectToPage("/Products/Details", new { id = movement.ProductId });
        }
        catch (ApiException exception) when (exception.IsUserCorrectable)
        {
            ModelState.AddApiErrors(exception, nameof(Form));
            await LoadProductsAsync(cancellationToken);

            return Page();
        }
    }
    // Only active products accept movements, so inactive ones are not offered. The stock is
    // shown next to each name, which makes an "Out" larger than the stock easy to avoid.
    private async Task LoadProductsAsync(CancellationToken cancellationToken)
    {
        PagedList<ProductModel> products = await _apiClient.GetProductsAsync(
            new ProductFilter(null, null, false, 1, ProductOptionLimit), cancellationToken);
        ProductOptions = products.Items
            .Select(product => new SelectListItem($"{product.Sku} · {product.Name} (stock {product.Stock})", product.Id.ToString()))
            .ToList();
    }
}
