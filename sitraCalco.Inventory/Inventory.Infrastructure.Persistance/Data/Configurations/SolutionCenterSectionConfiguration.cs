using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class SolutionCenterSectionConfiguration
        : IEntityTypeConfiguration<SolutionCenterSection>
    {
        public void Configure(EntityTypeBuilder<SolutionCenterSection> entity)
        {
            entity.HasKey(e => e.solution_center_section_id)
                .HasName("PRIMARY");

            entity.ToTable("sitracalco_inventory_solution_center_section");

            entity.Property(e => e.solution_center_section_id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.solution_center_id).IsRequired();
            entity.Property(e => e.section_id).IsRequired();
            entity.Property(e => e.is_active).IsRequired();

            entity.HasIndex(e => new { e.solution_center_id, e.section_id })
                .IsUnique();
        }
    }
}
