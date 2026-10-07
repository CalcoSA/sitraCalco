using FluentValidation;
using Inventory.Domain.Dtos;
using Inventory.Domain.Helpers;

namespace Inventory.Domain.Validators
{
    public class CreatePermissionDtoValidator : AbstractValidator<CreatePermissionDto>
    {
        public CreatePermissionDtoValidator()
        {
            RuleFor(x => x.PermissionKey)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("La clave del permiso es obligatoria.")
                .MaximumLength(150)
                .WithMessage("La clave del permiso no puede superar los 150 caracteres.");

            RuleFor(x => x.PermissionValue)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Los roles del permiso son obligatorios.")
                .MaximumLength(1000)
                .WithMessage("Los roles del permiso no pueden superar los 1000 caracteres.")
                .Must(value => PermissionValues.NormalizeRoles(value).Length > 0)
                .WithMessage("Debe indicar al menos un rol válido.");
        }
    }
}
