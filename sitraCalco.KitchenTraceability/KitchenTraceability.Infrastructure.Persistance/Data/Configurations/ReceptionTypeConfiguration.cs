using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data.Configurations
{
    public class ReceptionTypeConfiguration : IEntityTypeConfiguration<ReceptionType>
    {
        public void Configure(EntityTypeBuilder<ReceptionType> entity)
        {
            entity.HasKey(e => e.reception_type_id).HasName("PRIMARY");

            entity.ToTable("sitracalco_kitchentraceability_reception_type");

            entity.Property(e => e.form_code).HasMaxLength(30);
            entity.Property(e => e.is_active)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.name).HasMaxLength(100);
        }
    }
}