using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data
{
    public partial class SitraCalcoContext : DbContext
    {
        public SitraCalcoContext() { }
        public SitraCalcoContext(DbContextOptions<SitraCalcoContext> options) : base(options) { }

        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductReceptionConfiguration> ProductReceptionConfigurations { get; set; }
        public virtual DbSet<ProductTemperatureConfiguration> ProductTemperatureConfigurations { get; set; }
        public virtual DbSet<Reception> Receptions { get; set; }
        public virtual DbSet<ReceptionLot> ReceptionLots { get; set; }
        public virtual DbSet<ReceptionTemperatureMeasurement> ReceptionTemperatureMeasurements { get; set; }
        public virtual DbSet<ReceptionTransportEvaluation> ReceptionTransportEvaluations { get; set; }
        public virtual DbSet<ReceptionType> ReceptionTypes { get; set; }
        public virtual DbSet<ReceptionVisualInspection> ReceptionVisualInspections { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SitraCalcoContext).Assembly);
        }
    }
}