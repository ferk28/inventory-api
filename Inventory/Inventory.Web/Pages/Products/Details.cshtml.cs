using Inventory.Web.ApiClient;
using Inventory.Web.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Inventory.Web.Pages.Products;
public sealed class DetailsModel : PageModel
{
    private const int PageSize = 10;
    private readonly InventoryApiClient _apiClient;
    public DetailsModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    public ProductModel Product { get; private set; } = null!;
    public PagedList<MovementModel> Movements { get; private set; } = new([], 1, PageSize, 0);
    public PagerModel Pager => new(Movements.Page, Movements.TotalPages, Movements.TotalCount, new Dictionary<string, string?>
    {
        ["id"] = Product.Id.ToString()
    });
    public async Task OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Task<ProductModel> product = _apiClient.GetProductAsync(id, cancellationToken);
        Task<PagedList<MovementModel>> movements = _apiClient.GetProductMovementsAsync(id, Math.Max(PageNumber, 1), PageSize, cancellationToken);
        await Task.WhenAll(product, movements);
        Product = product.Result;
        Movements = movements.Result;
    }
}
