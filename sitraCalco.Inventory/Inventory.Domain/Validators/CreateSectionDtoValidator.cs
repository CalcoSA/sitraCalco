using FluentValidation;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Validators
{
    public class CreateSectionDtoValidator
        : AbstractValidator<CreateSectionDto>
    {
        public CreateSectionDtoValidator()
        {
            RuleFor(x => x.SectionName)
                .NotEmpty()
                .WithMessage(
                    "El nombre de la sección es obligatorio.")
                .Must(name =>
                    !string.IsNullOrWhiteSpace(name))
                .WithMessage(
                    "El nombre de la sección es obligatorio.")
                .MaximumLength(150)
                .WithMessage(
                    "El nombre de la sección no puede superar los 150 caracteres.");
        }
    }
}