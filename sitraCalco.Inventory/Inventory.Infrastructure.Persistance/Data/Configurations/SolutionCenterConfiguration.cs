using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistance.Data.Configurations
{
    public class SolutionCenterConfiguration
        : IEntityTypeConfiguration<SolutionCenter>
    {
        public void Configure(
            EntityTypeBuilder<SolutionCenter> entity)
        {
            entity.HasKey(e => e.solution_center_id)
                .HasName("PRIMARY");

            entity.ToTable(
                "sitracalco_inventory_solution_center");

            entity.Property(e => e.solution_center_id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.solution_center_code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.solution_center_name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.is_active)
                .IsRequired();

            entity.HasIndex(e => e.solution_center_code)
                .IsUnique();

            entity.HasOne<SolutionCenterType>()
                .WithMany()
                .HasForeignKey(e => e.solution_center_type_id)
                .HasConstraintName("fk_solution_center_type");
        }
    }
}