using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class CreateProductReceptionConfigurationValidator : AbstractValidator<CreateProductReceptionConfigurationDto>
    {
        public CreateProductReceptionConfigurationValidator()
        {
            RuleFor(x => x.product_id)
                .GreaterThan(0)
                .WithMessage("El producto es obligatorio.");

            RuleFor(x => x.minimum_shelf_life_days)
                .GreaterThanOrEqualTo(0)
                .When(x => x.minimum_shelf_life_days.HasValue)
                .WithMessage("Los días mínimos de vida útil no pueden ser negativos.");
        }
    }
}