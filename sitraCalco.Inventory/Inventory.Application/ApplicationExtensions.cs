using Microsoft.Extensions.DependencyInjection;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
//using Inventory.Domain.Validators;
using Inventory.Domain.Dtos;
using FluentValidation;

namespace Inventory.Application
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IProductApplication, ProductApplication>();
            services.AddScoped<ISolutionCenterApplication,SolutionCenterApplication>();

            //services.AddScoped<IValidator<CreateRoleDto>, CreateRoleDtoValidator>();


            return services;
        }
    }
}