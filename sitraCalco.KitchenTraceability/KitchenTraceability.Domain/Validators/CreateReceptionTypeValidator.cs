using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class CreateReceptionTypeValidator : AbstractValidator<CreateReceptionTypeDto>
    {
        public CreateReceptionTypeValidator()
        {
            RuleFor(x => x.name)
                .NotEmpty()
                .WithMessage("El nombre del tipo de recepción es obligatorio.")
                .MaximumLength(100)
                .WithMessage("El nombre del tipo de recepción no puede superar los 100 caracteres.");

            RuleFor(x => x.form_code)
                .MaximumLength(30)
                .When(x => !string.IsNullOrEmpty(x.form_code))
                .WithMessage("El código del formulario no puede superar los 30 caracteres.");
        }
    }
}