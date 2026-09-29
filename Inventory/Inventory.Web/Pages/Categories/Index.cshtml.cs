using Inventory.Web.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages.Categories;
public sealed class IndexModel : PageModel
{
    private readonly InventoryApiClient _apiClient;
    public IndexModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; }
    public IReadOnlyList<CategoryModel> Categories { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await _apiClient.GetCategoriesAsync(IncludeInactive, cancellationToken);
    }
}
