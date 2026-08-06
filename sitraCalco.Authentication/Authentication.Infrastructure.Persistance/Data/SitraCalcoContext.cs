using Microsoft.EntityFrameworkCore;
using Authentication.Domain.Models;

namespace Authentication.Infrastructure.Persistance.Data
{
    public partial class SitraCalcoContext : DbContext
    {
        public SitraCalcoContext() { }
        public SitraCalcoContext(DbContextOptions<SitraCalcoContext> options) : base(options) { }

        public virtual DbSet<Menuoption> Menuoptions { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Rolemenuoption> Rolemenuoptions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Userrole> Userroles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SitraCalcoContext).Assembly);
        }
    }
}