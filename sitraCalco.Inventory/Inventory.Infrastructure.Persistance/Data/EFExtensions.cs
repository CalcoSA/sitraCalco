using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Persistance.Data;
using Inventory.Infrastructure.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Persistance.Data
{
    public static class EFExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SitraCalcoDatabase");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No se encontró la cadena de conexión 'SitraCalcoDatabase'.");
            }

            services.AddDbContext<SitraCalcoContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            //services.AddAutoMapper(config =>
            //{
            //    config.AddProfile<AutoMapperProfile>();
            //});

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ISiesaRepository, SiesaRepository>();
            services.AddScoped<ISolutionCenterRepository,SolutionCenterRepository>();


            return services;
        }
    }
}