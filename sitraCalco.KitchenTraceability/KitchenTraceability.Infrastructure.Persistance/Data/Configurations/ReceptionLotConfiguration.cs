using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ReceptionLotConfiguration : IEntityTypeConfiguration<ReceptionLot>
    {
        public void Configure(EntityTypeBuilder<ReceptionLot> entity)
        {
            entity.HasKey(e => e.reception_lot_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_reception_lot");

            entity.HasIndex(e => e.lot_number, "idx_ktr_lot_number");

            entity.HasIndex(e => e.product_reference, "idx_ktr_lot_product_reference");

            entity.HasIndex(e => e.reception_id, "idx_ktr_lot_reception");

            entity.Property(e => e.created_at)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.lot_number).HasMaxLength(100);
            entity.Property(e => e.lot_size).HasPrecision(18, 3);
            entity.Property(e => e.lot_status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'");
            entity.Property(e => e.product_reference).HasMaxLength(100);
            entity.Property(e => e.unit_of_measure).HasMaxLength(20);

            entity.HasOne(d => d.reception).WithMany(p => p.receptionLots)
                .HasForeignKey(d => d.reception_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_lot_reception");
        }
    }
}