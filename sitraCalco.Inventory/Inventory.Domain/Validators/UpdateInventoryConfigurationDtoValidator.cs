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

            RuleFor(x => x.StartDay).NotNull().InclusiveBetween(1, 31)
                .When(x => x.HasStartDay);
            RuleFor(x => x.EndDay).NotNull().InclusiveBetween(1, 31)
                .When(x => x.HasEndDay);

            RuleFor(x => x)
                .Must(request =>
                    !request.StartDay.HasValue ||
                    !request.EndDay.HasValue ||
                    request.StartDay.Value <= request.EndDay.Value)
                .WithMessage(
                    "startDay no puede superar endDay.");
        }
    }
}
