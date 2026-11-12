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
                .NotNull()
                .When(x => x.requires_shelf_life == true)
                .WithMessage("Debe ingresar el mínimo de días de vida útil cuando el producto requiere validación de vida útil.");

            RuleFor(x => x.minimum_shelf_life_days)
                .GreaterThan(0)
                .When(x => x.requires_shelf_life == true && x.minimum_shelf_life_days.HasValue)
                .WithMessage("El mínimo de días de vida útil debe ser mayor que cero.");

            RuleFor(x => x.minimum_shelf_life_days)
                .Null()
                .When(x => x.requires_shelf_life != true)
                .WithMessage("No debe ingresar el mínimo de días de vida útil si el producto no requiere validación de vida útil.");
        }
    }
}