using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class UpdateSupplierValidator : AbstractValidator<UpdateSupplierDto>
    {
        public UpdateSupplierValidator()
        {
            RuleFor(x => x.supplier_code)
                .NotEmpty()
                .WithMessage("El código del proveedor es obligatorio.")
                .MaximumLength(100)
                .WithMessage("El código del proveedor no puede superar los 100 caracteres.");

            RuleFor(x => x.name)
                .NotEmpty()
                .WithMessage("El nombre del proveedor es obligatorio.")
                .MaximumLength(255)
                .WithMessage("El nombre del proveedor no puede superar los 255 caracteres.");
        }
    }
}