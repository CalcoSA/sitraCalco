using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class InventoryConfigurationConfiguration
        : IEntityTypeConfiguration<InventoryConfiguration>
    {
        public void Configure(
            EntityTypeBuilder<InventoryConfiguration> entity)
        {
            entity.HasKey(
                    e => e.inventory_configuration_id)
                .HasName("PRIMARY");

            entity.ToTable(
                "sitracalco_inventory_configuration");

            entity.Property(
                    e => e.inventory_configuration_id)
                .ValueGeneratedOnAdd();

            entity.Property(
                    e => e.inventory_configuration_name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(
                    e => e.start_date)
                .HasColumnType("date");

            entity.Property(
                    e => e.end_date)
                .HasColumnType("date");

            entity.HasIndex(
                    e => e.inventory_configuration_name)
                .IsUnique()
                .HasDatabaseName(
                    "uq_inventory_configuration_name");
        }
    }
}