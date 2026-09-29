using Inventory.Web.ApiClient;
using Inventory.Web.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Inventory.Web.Pages.Movements;
public sealed class IndexModel : PageModel
{
    private const int PageSize = 20;
    private const int ProductOptionLimit = 100;
    private readonly InventoryApiClient _apiClient;
    public IndexModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty(SupportsGet = true)]
    public int? ProductId { get; set; }
    [BindProperty(SupportsGet = true)]
    public MovementType? Type { get; set; }
    [BindProperty(SupportsGet = true)]
    public DateOnly? From { get; set; }
    [BindProperty(SupportsGet = true)]
    public DateOnly? To { get; set; }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    public PagedList<MovementModel> Movements { get; private set; } = new([], 1, PageSize, 0);
    public IReadOnlyList<SelectListItem> ProductOptions { get; private set; } = [];
    public string? FilterError { get; private set; }
    public bool IsFiltered => ProductId is not null || Type is not null || From is not null || To is not null;
    public PagerModel Pager => new(Movements.Page, Movements.TotalPages, Movements.TotalCount, new Dictionary<string, string?>
    {
        ["productId"] = ProductId?.ToString(),
        ["type"] = Type?.ToString(),
        ["from"] = From?.ToString("yyyy-MM-dd"),
        ["to"] = To?.ToString("yyyy-MM-dd")
    });
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Task<PagedList<ProductModel>> products = _apiClient.GetProductsAsync(
            new ProductFilter(null, null, true, 1, ProductOptionLimit), cancellationToken);
        if (From is not null && To is not null && From > To)
        {
            FilterError = "The start date must be on or before the end date.";
        }
        else
        {
            MovementFilter filter = new(ProductId, Type, StartOfDayUtc(From), EndOfDayUtc(To), Math.Max(PageNumber, 1), PageSize);
            Movements = await _apiClient.GetMovementsAsync(filter, cancellationToken);
        }
        ProductOptions = (await products).Items
            .Select(product => new SelectListItem($"{product.Sku} · {product.Name}", product.Id.ToString()))
            .ToList();
    }
    // The dates in the filter are the user's local days; the API compares against UTC.
    private static DateTime? StartOfDayUtc(DateOnly? day)
    {
        return day?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
    }
    private static DateTime? EndOfDayUtc(DateOnly? day)
    {
        return day?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime().AddTicks(-1);
    }
}
