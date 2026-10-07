using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ReceptionTemperatureMeasurementConfiguration : IEntityTypeConfiguration<ReceptionTemperatureMeasurement>
    {
        public void Configure(EntityTypeBuilder<ReceptionTemperatureMeasurement> entity)
        {
            entity.HasKey(e => e.temperature_measurement_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_reception_temperature_measurement");

            entity.HasIndex(e => e.reception_lot_id, "idx_ktr_temperature_lot");

            entity.Property(e => e.comments).HasColumnType("text");
            entity.Property(e => e.measured_at).HasColumnType("datetime");
            entity.Property(e => e.measured_by).HasMaxLength(100);
            entity.Property(e => e.measurement_point).HasMaxLength(20);
            entity.Property(e => e.temperature_celsius).HasPrecision(5, 2);

            entity.HasOne(d => d.receptionLot).WithMany(p => p.receptionTemperatureMeasurements)
                .HasForeignKey(d => d.reception_lot_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ktr_temperature_lot");
        }
    }
}