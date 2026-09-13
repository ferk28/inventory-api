namespace Inventory.Application.Common.Models;
public sealed record PagedResult<TItem>(IReadOnlyCollection<TItem> Items, int Page, int PageSize, int TotalCount);
