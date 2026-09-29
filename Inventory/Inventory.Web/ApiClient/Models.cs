namespace Inventory.Web.ApiClient;
// The web app keeps its own copy of the wire contract instead of referencing the API's
// DTOs: it only knows the API through HTTP, exactly like any other client would.
public enum MovementType
{
    In,
    Out
}
public sealed record PagedList<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
public sealed record ProductModel(
    int Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
public sealed record CategoryModel(int Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);
public sealed record MovementModel(
    int Id,
    int ProductId,
    string ProductSku,
    MovementType Type,
    int Quantity,
    string? Reason,
    int? StockAfter,
    DateTime CreatedAt);
public sealed record ProductFilter(string? Search, int? CategoryId, bool IncludeInactive, int Page, int PageSize);
public sealed record MovementFilter(int? ProductId, MovementType? Type, DateTime? From, DateTime? To, int Page, int PageSize);
public sealed record CreateProductRequest(string Sku, string Name, string? Description, decimal Price, int CategoryId);
public sealed record UpdateProductRequest(string Name, string? Description, decimal Price, int CategoryId);
public sealed record CategoryRequest(string Name, string? Description);
public sealed record RegisterMovementRequest(int ProductId, MovementType Type, int Quantity, string? Reason);
