using Microsoft.Extensions.DependencyInjection;

namespace KitchenTraceability.Application
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            return services;
        }
    }
}