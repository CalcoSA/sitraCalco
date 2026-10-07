using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ProductTemperatureConfigurationConfiguration : IEntityTypeConfiguration<ProductTemperatureConfiguration>
    {
        public void Configure(EntityTypeBuilder<ProductTemperatureConfiguration> entity)
        {
            entity.HasKey(e => e.temperature_configuration_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_product_temperature_configuration");

            entity.HasIndex(e => e.product_id, "idx_ktr_ptc_product");

            entity.Property(e => e.conditional_max_temperature).HasPrecision(5, 2);
            entity.Property(e => e.conditional_min_temperature).HasPrecision(5, 2);
            entity.Property(e => e.created_at)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.created_by).HasMaxLength(100);
            entity.Property(e => e.ideal_max_temperature).HasPrecision(5, 2);
            entity.Property(e => e.ideal_min_temperature).HasPrecision(5, 2);
            entity.Property(e => e.rejection_max_temperature).HasPrecision(5, 2);
            entity.Property(e => e.rejection_min_temperature).HasPrecision(5, 2);
            entity.Property(e => e.updated_at).HasColumnType("datetime");
            entity.Property(e => e.updated_by).HasMaxLength(100);

            entity.HasOne(d => d.product).WithMany(p => p.productTemperatureConfigurations)
                .HasForeignKey(d => d.product_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_ptc_product");
        }
    }
}