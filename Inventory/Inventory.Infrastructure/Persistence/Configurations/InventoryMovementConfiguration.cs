using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Inventory.Infrastructure.Persistence.Configurations;
public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Type).HasConversion<byte>().IsRequired();
        builder.Property(movement => movement.Quantity).IsRequired();
        builder.Property(movement => movement.StockAfter);
        builder.Property(movement => movement.Reason).HasMaxLength(250);
        builder.Property(movement => movement.CreatedAt).IsRequired();
        builder.HasIndex(movement => new { movement.ProductId, movement.CreatedAt });
        builder.HasOne(movement => movement.Product)
            .WithMany()
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
