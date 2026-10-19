using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Authentication.Domain.Models;

namespace Authentication.Infrastructure.Persistance.Data.Configurations
{
    internal class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> entity)
        {
            entity.HasKey(e => e.IdRole).HasName("PRIMARY");

            entity.ToTable("sitracalco_authentication_role");

            entity.HasIndex(e => e.NameRole, "uq_sitracalco_authentication_name").IsUnique();

            entity.HasIndex(e => e.RoleCode, "uq_auth_role_code").IsUnique();
            entity.Property(e => e.RoleCode)
                .HasColumnName("role_code")
                .HasColumnType("varchar(32)")
                .HasMaxLength(32)
                .HasCharSet("ascii")
                .UseCollation("ascii_bin");

            entity.Property(e => e.NameRole)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("nameRole");
            entity.Property(e => e.StatusRole)
                .HasDefaultValueSql("'1'")
                .HasColumnName("statusRole");
        }
    }
}
