using FluentValidation;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Validators
{
    public class CreateInventoryConfigurationDtoValidator
        : AbstractValidator<CreateInventoryConfigurationDto>
    {
        public CreateInventoryConfigurationDtoValidator()
        {
            RuleFor(x => x.InventoryConfigurationName)
                .NotEmpty()
                .WithMessage(
                    "El nombre de la configuración de inventario es obligatorio.")
                .Must(name =>
                    !string.IsNullOrWhiteSpace(name))
                .WithMessage(
                    "El nombre de la configuración de inventario es obligatorio.")
                .MaximumLength(100)
                .WithMessage(
                    "El nombre de la configuración de inventario no puede superar los 100 caracteres.");

            RuleFor(x => x)
                .Must(request =>
                    !request.StartDate.HasValue ||
                    !request.EndDate.HasValue ||
                    request.StartDate.Value.Date <=
                    request.EndDate.Value.Date)
                .WithMessage(
                    "La fecha de inicio no puede ser posterior a la fecha de fin.");
        }
    }
}