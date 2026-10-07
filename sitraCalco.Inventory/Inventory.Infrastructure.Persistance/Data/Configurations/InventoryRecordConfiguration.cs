using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class InventoryRecordConfiguration : IEntityTypeConfiguration<InventoryRecord>
    {
        public void Configure(EntityTypeBuilder<InventoryRecord> entity)
        {
            entity.HasKey(e => e.inventory_id).HasName("PRIMARY");
            entity.ToTable("sitracalco_inventory_inventory");

            entity.Property(e => e.inventory_id).ValueGeneratedOnAdd();

            // MySqlConnector lee CHAR(36) como Guid; el modelo conserva el contrato string.
            entity.Property(e => e.inventory_execution_id)
                .HasConversion(
                    value => Guid.Parse(value),
                    value => value.ToString())
                .HasColumnType("char(36)")
                .IsRequired();

            entity.Property(e => e.solution_center_code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.solution_center_name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.inventory_configuration_name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.section_name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.reference).HasMaxLength(100).IsRequired();
            entity.Property(e => e.product_name).HasMaxLength(500).IsRequired();
            entity.Property(e => e.unit_of_measure).HasMaxLength(200).IsRequired();
            entity.Property(e => e.plan_id).HasMaxLength(50);

            // Pomelo mapea byte a tinyint unsigned por convención.
            entity.Property(e => e.count_number).IsRequired();

            entity.Property(e => e.count_value).HasPrecision(18, 4);
            entity.Property(e => e.open).HasPrecision(18, 4);
            entity.Property(e => e.closed).HasPrecision(18, 4);
            entity.Property(e => e.multiplication_value).HasPrecision(18, 4);
            entity.Property(e => e.entered_by).HasMaxLength(150).IsRequired();

            entity.Property(e => e.created_at)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .ValueGeneratedOnAdd()
                .IsRequired();

            entity.HasIndex(e => new
                {
                    e.inventory_execution_id,
                    e.count_number,
                    e.product_id,
                    e.unit_of_measure
                })
                .IsUnique()
                .HasDatabaseName("uq_inventory_execution_count_product_uom");
        }
    }
}
