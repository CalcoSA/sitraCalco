using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Inventory.Infrastructure.Persistance.Data
{
    public partial class SitraCalcoContext : DbContext
    {
        public SitraCalcoContext() { }
        public SitraCalcoContext(DbContextOptions<SitraCalcoContext> options) : base(options) { }

        public virtual DbSet<Product> Products { get; set; }

        public virtual DbSet<SolutionCenterType> SolutionCenterTypes { get; set; }

        public virtual DbSet<SolutionCenter> SolutionCenters { get; set; }

        public virtual DbSet<Section>Sections { get; set; }

        public virtual DbSet<SolutionCenterProduct> SolutionCenterProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SitraCalcoContext).Assembly);
        }
    }
}