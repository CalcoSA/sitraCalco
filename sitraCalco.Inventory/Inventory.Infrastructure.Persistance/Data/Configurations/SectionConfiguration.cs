using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class SectionConfiguration
        : IEntityTypeConfiguration<Section>
    {
        public void Configure(
            EntityTypeBuilder<Section> entity)
        {
            entity.HasKey(e => e.section_id)
                .HasName("PRIMARY");

            entity.ToTable(
                "sitracalco_inventory_section");

            entity.Property(e => e.section_id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.section_name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.is_active)
                .IsRequired();

            entity.Property(e => e.solution_center_type_id)
                .IsRequired();

            entity.HasIndex(e => e.solution_center_type_id)
                .HasDatabaseName("ix_section_solution_center_type_id");

            entity.HasOne(section => section.SolutionCenterType)
                .WithMany()
                .HasForeignKey(e => e.solution_center_type_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_section_solution_center_type");
        }
    }
}
