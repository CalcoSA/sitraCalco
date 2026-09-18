using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class InventoryConfigurationAssignmentConfiguration
        : IEntityTypeConfiguration<InventoryConfigurationAssignment>
    {
        public void Configure(
            EntityTypeBuilder<InventoryConfigurationAssignment> entity)
        {
            entity.HasKey(
                    e => e.inventory_configuration_assignment_id)
                .HasName("PRIMARY");

            entity.ToTable(
                "sitracalco_inventory_configuration_assignment");

            entity.Property(
                    e => e.inventory_configuration_assignment_id)
                .ValueGeneratedOnAdd();

            entity.Property(
                    e => e.inventory_configuration_id)
                .IsRequired();

            entity.Property(
                    e => e.solution_center_id)
                .IsRequired();

            entity.Property(
                    e => e.section_id)
                .IsRequired();

            entity.Property(
                    e => e.is_active)
                .IsRequired();
        }
    }
}