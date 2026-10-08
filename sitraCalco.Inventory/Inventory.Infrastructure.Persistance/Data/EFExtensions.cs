using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Persistance.Data;
using Inventory.Domain.Options;
using Inventory.Infrastructure.Persistance.Repositories;
using Inventory.Infrastructure.Persistance.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            services.AddOptions<GoogleCloudStorageOptions>()
                .Validate(options =>
                    string.Equals(options.AuthenticationMode, "Adc", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(options.AuthenticationMode, "Hmac", StringComparison.OrdinalIgnoreCase),
                    "GoogleCloudStorage:AuthenticationMode debe ser Adc o Hmac.")
                .Validate(options =>
                    !string.Equals(options.AuthenticationMode, "Hmac", StringComparison.OrdinalIgnoreCase) ||
                    GoogleCloudStorageHmacService.HasValidConfiguration(options),
                    "La configuración HMAC de Google Cloud Storage está incompleta o no utiliza un endpoint HTTPS válido.");

            services.AddSingleton<IStorageService>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<GoogleCloudStorageOptions>>();
                if (string.Equals(options.Value.AuthenticationMode, "Hmac", StringComparison.OrdinalIgnoreCase))
                    return new GoogleCloudStorageHmacService(options,
                        provider.GetRequiredService<ILogger<GoogleCloudStorageHmacService>>());

                return new GoogleCloudStorageService(options);
            });
            services.AddScoped<ISiesaRepository, SiesaRepository>();
            services.AddScoped<ISolutionCenterRepository,SolutionCenterRepository>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<ISectionRepository, SectionRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IInventoryConfigurationRepository,InventoryConfigurationRepository>();
            services.AddScoped<IInventoryRepository, InventoryRepository>();

            return services;
        }
    }
}
