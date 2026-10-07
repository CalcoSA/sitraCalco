using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ReceptionConfiguration : IEntityTypeConfiguration<Reception>
    {
        public void Configure(EntityTypeBuilder<Reception> entity)
        {
            entity.HasKey(e => e.reception_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_reception");

            entity.HasIndex(e => e.reception_date, "idx_ktr_reception_date");

            entity.HasIndex(e => e.supplier_id, "idx_ktr_reception_supplier");

            entity.HasIndex(e => e.reception_type_id, "idx_ktr_reception_type");

            entity.HasIndex(e => e.reception_number, "uk_ktr_reception_number").IsUnique();

            entity.Property(e => e.comments).HasColumnType("text");
            entity.Property(e => e.created_at)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.guide_number).HasMaxLength(100);
            entity.Property(e => e.operator_user).HasMaxLength(100);
            entity.Property(e => e.reception_date).HasColumnType("datetime");
            entity.Property(e => e.reception_number).HasMaxLength(30);

            entity.HasOne(d => d.receptionType).WithMany(p => p.receptions)
                .HasForeignKey(d => d.reception_type_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_reception_type");

            entity.HasOne(d => d.supplier).WithMany(p => p.receptions)
                .HasForeignKey(d => d.supplier_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_reception_supplier");
        }
    }
}