using Inventory.Domain.Enums;
namespace Inventory.Application.InventoryMovements.Queries;
public sealed record MovementDto(
    int Id,
    int ProductId,
    string ProductSku,
    MovementType Type,
    int Quantity,
    string? Reason,
    int? StockAfter,
    DateTime CreatedAt);
