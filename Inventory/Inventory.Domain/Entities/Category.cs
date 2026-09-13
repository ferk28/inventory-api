namespace Inventory.Domain.Entities;
public class Category
{
    private readonly List<Product> _products = new();
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();
    private Category()
    {
    }
    public Category(string name, string? description)
    {
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
    public void Rename(string name, string? description)
    {
        Name = name;
        Description = description;
    }
    public bool HasActiveProducts()
    {
        return _products.Any(product => product.IsActive);
    }
}
