using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Inventory.Infrastructure.Persistence.Configurations;
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Sku).HasMaxLength(50).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(150).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(500);
        builder.Property(product => product.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(product => product.Stock).IsRequired();
        builder.Property(product => product.IsActive).IsRequired();
        builder.Property(product => product.CreatedAt).IsRequired();
        builder.HasIndex(product => product.Sku).IsUnique();
        builder.HasIndex(product => product.CategoryId);
    }
}
