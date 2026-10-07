using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ReceptionTransportEvaluationConfiguration : IEntityTypeConfiguration<ReceptionTransportEvaluation>
    {
        public void Configure(EntityTypeBuilder<ReceptionTransportEvaluation> entity)
        {
            entity.HasKey(e => e.transport_evaluation_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_reception_transport_evaluation");

            entity.HasIndex(e => e.reception_id, "idx_ktr_transport_reception");

            entity.Property(e => e.comments).HasColumnType("text");
            entity.Property(e => e.evaluated_at).HasColumnType("datetime");
            entity.Property(e => e.evaluated_by).HasMaxLength(100);

            entity.HasOne(d => d.reception).WithMany(p => p.receptionTransportEvaluations)
                .HasForeignKey(d => d.reception_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_transport_reception");
        }
    }
}