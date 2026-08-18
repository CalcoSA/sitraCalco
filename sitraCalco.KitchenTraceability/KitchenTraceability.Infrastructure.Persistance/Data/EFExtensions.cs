using KitchenTraceability.Infrastructure.Persistance.Repositories;
using KitchenTraceability.Infrastructure.Persistance.Mappings;
using Microsoft.Extensions.DependencyInjection;
using KitchenTraceability.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Data
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

            services.AddAutoMapper(config =>
            {
                config.AddProfile<AutoMapperProfile>();
            });

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }
    }
}