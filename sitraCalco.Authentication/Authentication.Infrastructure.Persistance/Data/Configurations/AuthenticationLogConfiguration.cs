using Authentication.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authentication.Infrastructure.Persistance.Data.Configurations
{
    public class AuthenticationLogConfiguration : IEntityTypeConfiguration<AuthenticationLog>
    {
        public void Configure(EntityTypeBuilder<AuthenticationLog> entity)
        {
            entity.HasKey(e => e.LogId).HasName("PRIMARY");

            entity.ToTable("sitracalco_authentication_log");

            entity.Property(e => e.LogId)
                .HasColumnName("log_id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Action)
                .HasColumnName("action")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Module)
                .HasColumnName("module")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.UserName)
                .HasColumnName("user_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd()
                .IsRequired();
        }
    }
}
