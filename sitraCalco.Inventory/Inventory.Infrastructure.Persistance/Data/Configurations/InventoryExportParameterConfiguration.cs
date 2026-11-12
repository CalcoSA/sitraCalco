using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class InventoryExportParameterConfiguration : IEntityTypeConfiguration<InventoryExportParameter>
    {
        public void Configure(EntityTypeBuilder<InventoryExportParameter> entity)
        {
            entity.ToTable("sitracalco_inventory_export_parameter");
            entity.HasKey(e => e.export_parameter_id).HasName("PRIMARY");
            entity.Property(e => e.export_parameter_id).HasColumnType("bigint").ValueGeneratedOnAdd();

            entity.Property(e => e.configuration_code).HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
            entity.Property(e => e.configuration_name).HasColumnType("varchar(200)").HasMaxLength(200).IsRequired();

            entity.Property(e => e.detail_record_type).HasColumnType("varchar(4)").HasMaxLength(4).IsRequired();
            entity.Property(e => e.detail_record_subtype).HasColumnType("varchar(2)").HasMaxLength(2).IsRequired();
            entity.Property(e => e.detail_record_version).HasColumnType("varchar(2)").HasMaxLength(2).IsRequired();
            entity.Property(e => e.company_code).HasColumnType("varchar(10)").HasMaxLength(10).IsRequired();

            entity.Property(e => e.reserved_field).HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
            entity.Property(e => e.auxiliary_location).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
            entity.Property(e => e.lot_code).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
            entity.Property(e => e.label_number).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();

            entity.Property(e => e.count_1_1).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
            entity.Property(e => e.count_2_1).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
            entity.Property(e => e.count_1_2).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
            entity.Property(e => e.count_2_2).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
            entity.Property(e => e.count_3_2).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();

            entity.Property(e => e.barcode).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
            entity.Property(e => e.detail_extension_1).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
            entity.Property(e => e.detail_extension_2).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();

            entity.Property(e => e.header_record_type).HasColumnType("varchar(4)").HasMaxLength(4).IsRequired();
            entity.Property(e => e.header_record_subtype).HasColumnType("varchar(2)").HasMaxLength(2).IsRequired();
            entity.Property(e => e.header_record_version).HasColumnType("varchar(2)").HasMaxLength(2).IsRequired();
            entity.Property(e => e.footer_record_type).HasColumnType("varchar(4)").HasMaxLength(4).IsRequired();
            entity.Property(e => e.footer_record_subtype).HasColumnType("varchar(2)").HasMaxLength(2).IsRequired();
            entity.Property(e => e.footer_record_version).HasColumnType("varchar(2)").HasMaxLength(2).IsRequired();

            // MySQL representa BOOLEAN como TINYINT(1).
            entity.Property(e => e.is_active).HasColumnType("tinyint(1)").HasDefaultValue(true).IsRequired();
            entity.Property(e => e.created_at).HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd().IsRequired();
            entity.Property(e => e.updated_at).HasColumnType("datetime(6)").IsRequired(false);

            entity.HasIndex(e => e.configuration_code).IsUnique()
                .HasDatabaseName("uq_export_configuration_code");
        }
    }
}
