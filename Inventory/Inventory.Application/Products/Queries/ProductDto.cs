namespace Inventory.Application.Products.Queries;
public sealed record ProductDto(
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
