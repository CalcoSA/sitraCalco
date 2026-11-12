using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> entity)
        {
            entity.HasKey(e => e.supplier_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_supplier");

            entity.HasIndex(e => e.supplier_code, "idx_ktr_supplier_code");

            entity.Property(e => e.name).HasMaxLength(255);
            entity.Property(e => e.supplier_code).HasMaxLength(100);
        }
    }
}