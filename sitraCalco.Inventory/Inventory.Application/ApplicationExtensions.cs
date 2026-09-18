using Microsoft.Extensions.DependencyInjection;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Validators;
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
            services.AddScoped<ILogApplication,LogApplication>();
            services.AddScoped<ISectionApplication,SectionApplication>();
            services.AddScoped<IValidator<CreateSectionDto>,CreateSectionDtoValidator>();
            services.AddScoped<IValidator<UpdateSectionDto>,UpdateSectionDtoValidator>();
            services.AddScoped<IInventoryConfigurationApplication,InventoryConfigurationApplication>();
            services.AddScoped<IValidator<CreateInventoryConfigurationDto>,CreateInventoryConfigurationDtoValidator>();
            services.AddScoped<IValidator<CreateInventoryConfigurationAssignmentsDto>,CreateInventoryConfigurationAssignmentsDtoValidator>();
            services.AddScoped<IValidator<UpdateInventoryConfigurationAssignmentStatusDto>,UpdateInventoryConfigurationAssignmentStatusDtoValidator>();
            services.AddScoped<IValidator<UpdateInventoryConfigurationDto>,UpdateInventoryConfigurationDtoValidator>();
            services.AddScoped<IValidator<AddInventoryConfigurationDaysDto>,AddInventoryConfigurationDaysDtoValidator>();

            return services;
        }
    }
}