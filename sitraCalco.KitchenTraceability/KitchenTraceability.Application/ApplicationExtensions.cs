using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using KitchenTraceability.Domain.Validators;
using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Application
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ISupplierApplication, SupplierApplication>();
            services.AddScoped<IProductApplication, ProductApplication>();
            services.AddScoped<IProductReceptionConfigurationApplication, ProductReceptionConfigurationApplication>();
            services.AddScoped<IProductTemperatureConfigurationApplication, ProductTemperatureConfigurationApplication>();
            services.AddScoped<IReceptionTypeApplication, ReceptionTypeApplication>();

            services.AddScoped<IValidator<CreateSupplierDto>, CreateSupplierValidator>();
            services.AddScoped<IValidator<UpdateSupplierDto>, UpdateSupplierValidator>();
            services.AddScoped<IValidator<PaginationDto>, PaginationValidator>();
            services.AddScoped<IValidator<CreateProductReceptionConfigurationDto>, CreateProductReceptionConfigurationValidator>();
            services.AddScoped<IValidator<UpdateProductReceptionConfigurationDto>, UpdateProductReceptionConfigurationValidator>();
            services.AddScoped<IValidator<CreateProductTemperatureConfigurationDto>, CreateProductTemperatureConfigurationValidator>();
            services.AddScoped<IValidator<UpdateProductTemperatureConfigurationDto>, UpdateProductTemperatureConfigurationValidator>();
            services.AddScoped<IValidator<CreateReceptionTypeDto>, CreateReceptionTypeValidator>();
            services.AddScoped<IValidator<UpdateReceptionTypeDto>, UpdateReceptionTypeValidator>();
            
            return services;
        }
    }
}