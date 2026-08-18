using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data
{
    public partial class SitraCalcoContext : DbContext
    {
        public SitraCalcoContext() { }
        public SitraCalcoContext(DbContextOptions<SitraCalcoContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SitraCalcoContext).Assembly);
        }
    }
}