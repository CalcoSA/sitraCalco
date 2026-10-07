using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ProductReceptionConfigurationConfiguration : IEntityTypeConfiguration<ProductReceptionConfiguration>
    {
        public void Configure(EntityTypeBuilder<ProductReceptionConfiguration> entity)
        {
            entity.HasKey(e => e.product_configuration_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_product_reception_configuration");

            entity.HasIndex(e => e.product_id, "idx_ktr_prc_product");

            entity.Property(e => e.created_at)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.created_by).HasMaxLength(100);
            entity.Property(e => e.updated_at).HasColumnType("datetime");
            entity.Property(e => e.updated_by).HasMaxLength(100);

            entity.HasOne(d => d.product).WithMany(p => p.productReceptionConfigurations)
                .HasForeignKey(d => d.product_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_prc_product");
        }
    }
}