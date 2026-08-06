using Authentication.Infrastructure.Persistance.Repositories;
using Authentication.Infrastructure.Persistance.Mappings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Authentication.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistance.Data
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
            services.AddScoped<IMenuOptionRepository, MenuOptionRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IWordpressUserRepository, WordpressUserRepository>();

            return services;
        }
    }
}