using Microsoft.Extensions.DependencyInjection;
using Authentication.Application.Interfaces;
using Authentication.Application.Services;
using Authentication.Domain.Validators;
using Authentication.Domain.Dtos;
using FluentValidation;

namespace Authentication.Application
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IMenuOptionApplication, MenuOptionApplication>();
            services.AddScoped<IRoleApplication, RoleApplication>();
            services.AddScoped<IUserApplication, UserApplication>();
            services.AddScoped<IJwtTokenApplication, JwtTokenApplication>();
            services.AddScoped<ISSOSignatureApplication, SSOSignatureApplication>();
            services.AddScoped<IAuthApplication, AuthApplication>();
            services.AddScoped<IWordpressUserApplication, WordpressUserApplication>();

            services.AddScoped<IValidator<CreateRoleDto>, CreateRoleDtoValidator>();
            services.AddScoped<IValidator<UpdateRoleDto>, UpdateRoleDtoValidator>();
            services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
            services.AddScoped<IValidator<UpdateUserDto>, UpdateUserDtoValidator>();

            return services;
        }
    }
}