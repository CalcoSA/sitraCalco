using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class SolutionCenterTypeConfiguration
        : IEntityTypeConfiguration<SolutionCenterType>
    {
        public void Configure(
            EntityTypeBuilder<SolutionCenterType> entity)
        {
            entity.HasKey(e => e.solution_center_type_id)
                .HasName("PRIMARY");

            entity.ToTable(
                "sitracalco_inventory_solution_center_type");

            entity.Property(e => e.solution_center_type_id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.solution_center_type_name)
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}