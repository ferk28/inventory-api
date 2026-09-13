namespace Inventory.Api.Contracts;
public sealed record UpdateProductRequest(string Name, string? Description, decimal Price, int CategoryId);
