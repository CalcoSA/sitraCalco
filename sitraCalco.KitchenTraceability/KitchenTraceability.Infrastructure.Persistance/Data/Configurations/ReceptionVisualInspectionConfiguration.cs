using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ReceptionVisualInspectionConfiguration : IEntityTypeConfiguration<ReceptionVisualInspection>
    {
        public void Configure(EntityTypeBuilder<ReceptionVisualInspection> entity)
        {
            entity.HasKey(e => e.visual_inspection_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_reception_visual_inspection");

            entity.HasIndex(e => e.reception_lot_id, "idx_ktr_visual_lot");

            entity.Property(e => e.comments).HasColumnType("text");
            entity.Property(e => e.compliant_units).HasPrecision(18, 3);
            entity.Property(e => e.evaluated_at).HasColumnType("datetime");
            entity.Property(e => e.evaluated_by).HasMaxLength(100);
            entity.Property(e => e.inspected_units).HasPrecision(18, 3);
            entity.Property(e => e.non_compliant_units).HasPrecision(18, 3);

            entity.HasOne(d => d.receptionLot).WithMany(p => p.receptionVisualInspections)
                .HasForeignKey(d => d.reception_lot_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_visual_lot");
        }
    }
}