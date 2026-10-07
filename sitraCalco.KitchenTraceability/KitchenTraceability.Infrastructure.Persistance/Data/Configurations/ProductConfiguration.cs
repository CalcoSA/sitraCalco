using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> entity)
        {
            entity.HasKey(e => e.product_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_product");

            entity.HasIndex(e => e.product_reference, "idx_ktr_product_reference");

            entity.Property(e => e.product_name).HasMaxLength(255);
            entity.Property(e => e.product_reference).HasMaxLength(100);
            entity.Property(e => e.unit_of_measure).HasMaxLength(100);
        }
    }
}