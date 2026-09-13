using Inventory.Application.Abstractions.Persistence;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Inventory.Infrastructure.Persistence;
public sealed class InventoryDbContext : DbContext, IInventoryReadContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }
    public DbSet<Category> CategorySet => Set<Category>();
    public DbSet<Product> ProductSet => Set<Product>();
    public DbSet<InventoryMovement> InventoryMovementSet => Set<InventoryMovement>();
    public IQueryable<Category> Categories => CategorySet.AsNoTracking();
    public IQueryable<Product> Products => ProductSet.AsNoTracking();
    public IQueryable<InventoryMovement> InventoryMovements => InventoryMovementSet.AsNoTracking();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
