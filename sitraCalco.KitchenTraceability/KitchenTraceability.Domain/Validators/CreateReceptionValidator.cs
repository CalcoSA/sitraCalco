using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class CreateReceptionValidator : AbstractValidator<CreateReceptionDto>
    {
        public CreateReceptionValidator()
        {
            RuleFor(x => x.reception_type_id)
                .GreaterThan(0)
                .WithMessage("El tipo de recepción es obligatorio.");

            RuleFor(x => x.supplier_id)
                .GreaterThan(0)
                .WithMessage("El proveedor es obligatorio.");

            RuleFor(x => x.comments)
                .NotEmpty()
                .When(x => !x.is_compliant)
                .WithMessage("Debe ingresar una observación cuando las condiciones de transporte no son aceptables.");
        }
    }
}