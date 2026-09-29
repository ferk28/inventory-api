using Inventory.Web.ApiClient;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages;
public sealed class IndexModel : PageModel
{
    private const int RecentMovementCount = 8;
    private readonly InventoryApiClient _apiClient;
    public IndexModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    public int ActiveProductCount { get; private set; }
    public int ActiveCategoryCount { get; private set; }
    public int MovementCount { get; private set; }
    public IReadOnlyList<MovementModel> RecentMovements { get; private set; } = [];
    // The three calls do not depend on each other, so they run at the same time and the
    // page waits for the slowest one instead of for their sum.
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Task<PagedList<ProductModel>> products = _apiClient.GetProductsAsync(new ProductFilter(null, null, false, 1, 1), cancellationToken);
        Task<IReadOnlyList<CategoryModel>> categories = _apiClient.GetCategoriesAsync(false, cancellationToken);
        Task<PagedList<MovementModel>> movements = _apiClient.GetMovementsAsync(
            new MovementFilter(null, null, null, null, 1, RecentMovementCount), cancellationToken);
        await Task.WhenAll(products, categories, movements);
        ActiveProductCount = products.Result.TotalCount;
        ActiveCategoryCount = categories.Result.Count;
        MovementCount = movements.Result.TotalCount;
        RecentMovements = movements.Result.Items;
    }
}
