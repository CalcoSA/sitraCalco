using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ConformityConfiguration : IEntityTypeConfiguration<Conformity>
    {
        public void Configure(EntityTypeBuilder<Conformity> entity)
        {
            entity.HasKey(e => e.nonconformity_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_nonconformity");

            entity.HasIndex(e => e.nonconformity_number, "uk_ktr_nonconformity_number").IsUnique();

            entity.HasIndex(e => e.reception_id, "idx_ktr_nonconformity_reception");

            entity.HasIndex(e => e.reception_lot_id, "idx_ktr_nonconformity_lot");

            entity.HasIndex(e => e.reception_type_id, "idx_ktr_nonconformity_reception_type");

            entity.HasIndex(e => e.reason_type, "idx_ktr_nonconformity_reason");

            entity.Property(e => e.nonconformity_number).HasMaxLength(30);
            entity.Property(e => e.reason_type).HasMaxLength(30);
            entity.Property(e => e.quantity).HasPrecision(18, 3);
            entity.Property(e => e.document_type).HasMaxLength(30);
            entity.Property(e => e.document_number).HasMaxLength(100);
            entity.Property(e => e.observations).HasColumnType("text");
            entity.Property(e => e.status).HasMaxLength(20).HasDefaultValueSql("'OPEN'");
            entity.Property(e => e.created_by).HasMaxLength(100);
            entity.Property(e => e.created_at).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("datetime");

            entity.HasOne(d => d.reception)
                .WithMany(p => p.conformities)
                .HasForeignKey(d => d.reception_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_nonconformity_reception");

            entity.HasOne(d => d.reception_lot)
                .WithMany(p => p.conformities)
                .HasForeignKey(d => d.reception_lot_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_nonconformity_lot");

            entity.HasOne(d => d.receptionType)
                .WithMany(p => p.conformities)
                .HasForeignKey(d => d.reception_type_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ktr_nonconformity_reception_type");
        }
    }
}