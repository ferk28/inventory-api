using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace Inventory.UnitTests.Application.ReadModel;
public sealed class InventoryReadDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Filename=:memory:");
    public InventoryReadDatabase()
    {
        _connection.Open();
        DbContextOptions<InventoryDbContext> options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new InventoryDbContext(options);
        Context.Database.EnsureCreated();
        Seed();
    }
    public InventoryDbContext Context { get; }
    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
    private void Seed()
    {
        Category peripherals = new("Peripherals", "Keyboards, mice and the like");
        Category storage = new("Storage", "Disks and memory cards");
        Category legacy = new("Legacy", "Discontinued lines");
        legacy.Deactivate();
        Context.AddRange(peripherals, storage, legacy);
        Context.SaveChanges();
        Product mouse = new("SKU-001", "Wireless mouse", "Ergonomic wireless mouse", 19.99m, peripherals.Id);
        Product keyboard = new("SKU-002", "Mechanical keyboard", "Blue switches", 59.90m, peripherals.Id);
        Product disk = new("SKU-003", "External disk", "1 TB portable disk", 74.00m, storage.Id);
        Product discontinued = new("SKU-004", "Ball mouse", "Discontinued model", 4.99m, peripherals.Id);
        discontinued.Deactivate();
        Context.AddRange(mouse, keyboard, disk, discontinued);
        Context.SaveChanges();
        SeedMovements(mouse, keyboard);
    }
    private void SeedMovements(Product mouse, Product keyboard)
    {
        InventoryMovement firstLoad = new(mouse.Id, MovementType.In, 30, "initial purchase");
        mouse.ApplyMovement(firstLoad);
        InventoryMovement sale = new(mouse.Id, MovementType.Out, 5, "sale");
        mouse.ApplyMovement(sale);
        InventoryMovement keyboardLoad = new(keyboard.Id, MovementType.In, 10, "initial purchase");
        keyboard.ApplyMovement(keyboardLoad);
        Context.AddRange(firstLoad, sale, keyboardLoad);
        Context.SaveChanges();
    }
}
