using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class CreateProductTemperatureConfigurationValidator : AbstractValidator<CreateProductTemperatureConfigurationDto>
    {
        public CreateProductTemperatureConfigurationValidator()
        {
            RuleFor(x => x.product_id)
                .GreaterThan(0)
                .WithMessage("El producto es obligatorio.");

            RuleFor(x => x.ideal_max_temperature)
                .GreaterThanOrEqualTo(x => x.ideal_min_temperature)
                .WithMessage("La temperatura máxima ideal debe ser mayor o igual a la temperatura mínima ideal.");

            RuleFor(x => x.conditional_max_temperature)
                .NotNull()
                .When(x => x.conditional_min_temperature.HasValue)
                .WithMessage("Debe ingresar la temperatura máxima condicionada.");

            RuleFor(x => x.conditional_min_temperature)
                .NotNull()
                .When(x => x.conditional_max_temperature.HasValue)
                .WithMessage("Debe ingresar la temperatura mínima condicionada.");

            RuleFor(x => x.conditional_max_temperature)
                .GreaterThanOrEqualTo(x => x.conditional_min_temperature)
                .When(x =>
                    x.conditional_min_temperature.HasValue &&
                    x.conditional_max_temperature.HasValue)
                .WithMessage("La temperatura máxima condicionada debe ser mayor o igual a la temperatura mínima condicionada.");

            RuleFor(x => x.rejection_max_temperature)
                .GreaterThanOrEqualTo(x => x.rejection_min_temperature)
                .When(x =>
                    x.rejection_min_temperature.HasValue &&
                    x.rejection_max_temperature.HasValue)
                .WithMessage("La temperatura máxima de rechazo debe ser mayor o igual a la temperatura mínima de rechazo.");
        }
    }
}