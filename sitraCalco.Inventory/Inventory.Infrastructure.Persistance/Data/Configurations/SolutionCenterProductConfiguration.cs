using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class SolutionCenterProductConfiguration
        : IEntityTypeConfiguration<SolutionCenterProduct>
    {
        public void Configure(
            EntityTypeBuilder<SolutionCenterProduct> entity)
        {
            entity.HasKey(e =>
                e.solution_center_product_id)
                .HasName("PRIMARY");

            entity.ToTable(
                "sitracalco_inventory_solution_center_product");

            entity.Property(e =>
                e.solution_center_product_id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.sort_order)
                .IsRequired();

            entity.Property(e => e.created_by)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.created_at)
                .IsRequired();
        }
    }
}