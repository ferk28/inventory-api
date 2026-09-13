namespace Inventory.Infrastructure.Persistence;
public sealed class DatabaseSettings
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Name { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
