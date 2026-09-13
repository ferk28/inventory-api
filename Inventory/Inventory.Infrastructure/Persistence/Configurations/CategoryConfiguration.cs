using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Inventory.Infrastructure.Persistence.Configurations;
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.Property(category => category.IsActive).IsRequired();
        builder.Property(category => category.CreatedAt).IsRequired();
        builder.HasIndex(category => category.Name).IsUnique();
        builder.HasMany(category => category.Products)
            .WithOne(product => product.Category)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(category => category.Products).HasField("_products");
    }
}
