using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Models;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> entity)
        {
            entity.HasKey(e => e.product_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_inventory_product");

            entity.Property(e => e.product_id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.image_path)
                .HasMaxLength(500);

            entity.Property(e => e.plan_id)
                .HasMaxLength(50);

            entity.Property(e => e.product_name)
                .HasMaxLength(255);

            entity.Property(e => e.reference)
                .HasMaxLength(100);

            entity.Property(e => e.unit_of_measure)
                .HasMaxLength(50);
        }
    }
}