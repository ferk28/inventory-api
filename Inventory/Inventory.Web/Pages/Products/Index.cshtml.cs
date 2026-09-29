using Inventory.Web.ApiClient;
using Inventory.Web.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Inventory.Web.Pages.Products;
public sealed class IndexModel : PageModel
{
    private const int PageSize = 15;
    private readonly InventoryApiClient _apiClient;
    public IndexModel(InventoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    public PagedList<ProductModel> Products { get; private set; } = new([], 1, PageSize, 0);
    public IReadOnlyList<SelectListItem> CategoryOptions { get; private set; } = [];
    public bool IsFiltered => !string.IsNullOrWhiteSpace(Search) || CategoryId is not null || IncludeInactive;
    public PagerModel Pager => new(Products.Page, Products.TotalPages, Products.TotalCount, new Dictionary<string, string?>
    {
        ["search"] = Search,
        ["categoryId"] = CategoryId?.ToString(),
        ["includeInactive"] = IncludeInactive ? "true" : null
    });
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ProductFilter filter = new(Search, CategoryId, IncludeInactive, Math.Max(PageNumber, 1), PageSize);
        Task<PagedList<ProductModel>> products = _apiClient.GetProductsAsync(filter, cancellationToken);
        Task<IReadOnlyList<CategoryModel>> categories = _apiClient.GetCategoriesAsync(true, cancellationToken);
        await Task.WhenAll(products, categories);
        Products = products.Result;
        CategoryOptions = categories.Result
            .Select(category => new SelectListItem(category.IsActive ? category.Name : $"{category.Name} (inactive)", category.Id.ToString()))
            .ToList();
    }
}
