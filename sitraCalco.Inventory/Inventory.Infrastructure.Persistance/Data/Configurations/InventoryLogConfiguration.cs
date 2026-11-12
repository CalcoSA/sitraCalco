using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class InventoryLogConfiguration
        : IEntityTypeConfiguration<InventoryLog>
    {
        public void Configure(
            EntityTypeBuilder<InventoryLog> entity)
        {
            entity.HasKey(e => e.log_id)
                .HasName("PRIMARY");

            entity.ToTable(
                "sitracalco_inventory_log");

            entity.Property(e => e.log_id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.action)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.module)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.user_name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.created_at)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd()
                .IsRequired();
        }
    }
}