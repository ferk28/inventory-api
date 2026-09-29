namespace Inventory.Web.ApiClient;
public sealed class InventoryApiSettings
{
    public const string SectionName = "InventoryApi";
    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 15;
}
