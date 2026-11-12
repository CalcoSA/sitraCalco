using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> entity)
        {
            entity.HasKey(e => e.permission_id).HasName("PRIMARY");
            entity.ToTable("sitracalco_inventory_permission");

            entity.Property(e => e.permission_id).ValueGeneratedOnAdd();
            entity.Property(e => e.permission_key).HasMaxLength(150).IsRequired();
            entity.Property(e => e.permission_value).HasMaxLength(1000).IsRequired();

            entity.HasIndex(e => e.permission_key).IsUnique();
        }
    }
}
