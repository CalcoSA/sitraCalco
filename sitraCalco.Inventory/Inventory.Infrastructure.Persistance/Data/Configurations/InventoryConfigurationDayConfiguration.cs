using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class InventoryConfigurationDayConfiguration
        : IEntityTypeConfiguration<InventoryConfigurationDay>
    {
        public void Configure(
            EntityTypeBuilder<InventoryConfigurationDay> entity)
        {
            entity.HasKey(
                    e => e.inventory_configuration_day_id)
                .HasName("PRIMARY");

            entity.ToTable(
                "sitracalco_inventory_configuration_day");

            entity.Property(
                    e => e.inventory_configuration_day_id)
                .ValueGeneratedOnAdd();

            entity.Property(
                    e => e.inventory_configuration_id)
                .IsRequired();

            entity.Property(
                    e => e.day_of_week)
                .HasColumnType(
                    "enum('Lunes','Martes','Miercoles','Jueves','Viernes','Sabado','Domingo')")
                .IsRequired();
        }
    }
}