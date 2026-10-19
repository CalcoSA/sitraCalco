using FluentValidation;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Validators
{
    public class UpdateSectionDtoValidator
        : AbstractValidator<UpdateSectionDto>
    {
        public UpdateSectionDtoValidator()
        {
            RuleFor(x => x.SolutionCenterTypeId)
                .GreaterThan(0)
                .When(x => x.SolutionCenterTypeId.HasValue)
                .WithMessage("El tipo de centro de solución debe ser mayor a cero.");

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
