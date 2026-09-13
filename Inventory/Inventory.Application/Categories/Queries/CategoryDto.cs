namespace Inventory.Application.Categories.Queries;
public sealed record CategoryDto(int Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);
