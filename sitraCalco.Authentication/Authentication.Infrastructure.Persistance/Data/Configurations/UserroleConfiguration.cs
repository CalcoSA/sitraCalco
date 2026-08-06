using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Authentication.Domain.Models;

namespace Authentication.Infrastructure.Persistance.Data.Configurations
{
    public class UserroleConfiguration : IEntityTypeConfiguration<Userrole>
    {
        public void Configure(EntityTypeBuilder<Userrole> entity)
        {
            entity.HasKey(e => e.IdUserRole).HasName("PRIMARY");

            entity.ToTable("sitracalco_authentication_userrole");

            entity.HasIndex(e => e.IdRole, "fk_sitracalco_authentication_user_role_role");

            entity.HasIndex(e => new { e.IdUser, e.IdRole }, "uq_sitracalco_authentication_user_role").IsUnique();

            entity.HasOne(d => d.IdRoleNavigation).WithMany(p => p.Userroles)
                .HasForeignKey(d => d.IdRole)
                .HasConstraintName("fk_sitracalco_authentication_user_role_role");

            entity.HasOne(d => d.IdUserNavigation).WithMany(p => p.Userroles)
                .HasForeignKey(d => d.IdUser)
                .HasConstraintName("fk_sitracalco_authentication_user_role_user");
        }
    }
}