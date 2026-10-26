using FluentValidation;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Validators
{
    public class UpdateInventoryConfigurationDtoValidator
        : AbstractValidator<UpdateInventoryConfigurationDto>
    {
        public UpdateInventoryConfigurationDtoValidator()
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
                    "El nombre de la configuración de inventario no puede superar los 100 caracteres.")
                .When(x => x.HasName);

            RuleFor(x => x.StartDay).InclusiveBetween(1, 31)
                .When(x => x.HasStartDay && x.StartDay.HasValue);
            RuleFor(x => x.EndDay).InclusiveBetween(1, 31)
                .When(x => x.HasEndDay && x.EndDay.HasValue);

            RuleFor(x => x)
                .Must(request =>
                    Helpers.MonthlyInventoryRange.IsValid(request.StartDay, request.EndDay))
                .WithMessage(
                    "startDay y endDay deben ser ambos NULL o formar un rango completo entre 1 y 31, con startDay menor o igual a endDay.")
                .When(x => x.HasStartDay && x.HasEndDay);
        }
    }
}
