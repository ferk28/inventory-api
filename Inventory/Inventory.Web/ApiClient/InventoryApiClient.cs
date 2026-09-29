using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
namespace Inventory.Web.ApiClient;
public sealed class InventoryApiClient
{
    private const string ProductsPath = "api/products";
    private const string CategoriesPath = "api/categories";
    private const string MovementsPath = "api/inventory/movements";
    private const string GenericFailure = "The inventory API could not complete the request.";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly HttpClient _httpClient;
    public InventoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public Task<PagedList<ProductModel>> GetProductsAsync(ProductFilter filter, CancellationToken cancellationToken)
    {
        Dictionary<string, string?> query = new()
        {
            ["search"] = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
            ["categoryId"] = filter.CategoryId?.ToString(CultureInfo.InvariantCulture),
            ["includeInactive"] = filter.IncludeInactive ? "true" : null,
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = filter.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        return GetAsync<PagedList<ProductModel>>(WithQuery(ProductsPath, query), cancellationToken);
    }
    public Task<ProductModel> GetProductAsync(int productId, CancellationToken cancellationToken)
    {
        return GetAsync<ProductModel>($"{ProductsPath}/{productId}", cancellationToken);
    }
    public Task<PagedList<MovementModel>> GetProductMovementsAsync(int productId, int page, int pageSize, CancellationToken cancellationToken)
    {
        Dictionary<string, string?> query = new()
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };

        return GetAsync<PagedList<MovementModel>>(WithQuery($"{ProductsPath}/{productId}/movements", query), cancellationToken);
    }
    public Task<ProductModel> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<ProductModel>(HttpMethod.Post, ProductsPath, request, cancellationToken);
    }
    public Task UpdateProductAsync(int productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        return SendAsync(HttpMethod.Put, $"{ProductsPath}/{productId}", request, cancellationToken);
    }
    public Task DeleteProductAsync(int productId, CancellationToken cancellationToken)
    {
        return SendAsync(HttpMethod.Delete, $"{ProductsPath}/{productId}", null, cancellationToken);
    }
    public Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        Dictionary<string, string?> query = new() { ["includeInactive"] = includeInactive ? "true" : null };

        return GetAsync<IReadOnlyList<CategoryModel>>(WithQuery(CategoriesPath, query), cancellationToken);
    }
    public Task<CategoryModel> GetCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return GetAsync<CategoryModel>($"{CategoriesPath}/{categoryId}", cancellationToken);
    }
    public Task<CategoryModel> CreateCategoryAsync(CategoryRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<CategoryModel>(HttpMethod.Post, CategoriesPath, request, cancellationToken);
    }
    public Task UpdateCategoryAsync(int categoryId, CategoryRequest request, CancellationToken cancellationToken)
    {
        return SendAsync(HttpMethod.Put, $"{CategoriesPath}/{categoryId}", request, cancellationToken);
    }
    public Task DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return SendAsync(HttpMethod.Delete, $"{CategoriesPath}/{categoryId}", null, cancellationToken);
    }
    public Task<PagedList<MovementModel>> GetMovementsAsync(MovementFilter filter, CancellationToken cancellationToken)
    {
        Dictionary<string, string?> query = new()
        {
            ["productId"] = filter.ProductId?.ToString(CultureInfo.InvariantCulture),
            ["type"] = filter.Type?.ToString(),
            ["from"] = FormatUtc(filter.From),
            ["to"] = FormatUtc(filter.To),
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = filter.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        return GetAsync<PagedList<MovementModel>>(WithQuery(MovementsPath, query), cancellationToken);
    }
    public Task<MovementModel> RegisterMovementAsync(RegisterMovementRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<MovementModel>(HttpMethod.Post, MovementsPath, request, cancellationToken);
    }
    private async Task<TResult> GetAsync<TResult>(string path, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(path, cancellationToken);

        return await ReadAsync<TResult>(response, cancellationToken);
    }
    private async Task<TResult> SendAsync<TResult>(HttpMethod method, string path, object body, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, path) { Content = JsonContent.Create(body, options: JsonOptions) };
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

        return await ReadAsync<TResult>(response, cancellationToken);
    }
    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }
    private static async Task<TResult> ReadAsync<TResult>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        TResult? result = await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, cancellationToken);

        return result ?? throw new ApiException(response.StatusCode, "The inventory API returned an empty response.");
    }
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        ProblemBody? problem = await TryReadProblemAsync(response, cancellationToken);
        string message = problem?.Detail ?? problem?.Title ?? GenericFailure;

        throw new ApiException(response.StatusCode, message, problem?.Errors);
    }
    // A 401 from the API or a 502 from a proxy has no ProblemDetails body, and that must
    // still surface as an ApiException with the right status, not as a JSON error.
    private static async Task<ProblemBody?> TryReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemBody>(JsonOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }
    private static string WithQuery(string path, Dictionary<string, string?> query)
    {
        IEnumerable<KeyValuePair<string, string?>> present = query.Where(pair => pair.Value is not null);

        return QueryHelpers.AddQueryString(path, present);
    }
    // Sent without an offset on purpose: model binding would convert a "Z" value to the API
    // host's local time, while the column it is compared with holds UTC.
    private static string? FormatUtc(DateTime? utc)
    {
        return utc?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
    }
    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
    private sealed record ProblemBody(string? Title, string? Detail, int? Status, Dictionary<string, string[]>? Errors);
}
