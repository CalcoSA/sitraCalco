using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class UpdateProductReceptionConfigurationValidator : AbstractValidator<UpdateProductReceptionConfigurationDto>
    {
        public UpdateProductReceptionConfigurationValidator()
        {
            RuleFor(x => x.minimum_shelf_life_days)
                .GreaterThanOrEqualTo(0)
                .When(x => x.minimum_shelf_life_days.HasValue)
                .WithMessage("Los días mínimos de vida útil no pueden ser negativos.");
        }
    }
}