using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Authentication.Domain.Models;

namespace Authentication.Infrastructure.Persistance.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            entity.HasKey(e => e.IdUser).HasName("PRIMARY");

            entity.ToTable("sitracalco_authentication_users");

            entity.HasIndex(e => e.UserLogin, "uq_sitracalco_authentication_users_user_login").IsUnique();

            entity.HasIndex(e => e.WordpressUserId, "uq_sitracalco_authentication_users_user_wordpress_id").IsUnique();

            entity.Property(e => e.StatusUser)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("statusUser");
            entity.Property(e => e.UserLogin)
                .IsRequired()
                .HasMaxLength(60)
                .HasColumnName("userLogin");
            entity.Property(e => e.UserName)
                .HasMaxLength(250)
                .HasColumnName("userName");
            entity.Property(e => e.WordpressUserId).HasColumnName("wordpressUserId");
        }
    }
}