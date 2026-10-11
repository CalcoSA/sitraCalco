using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class CreateReceptionLotValidator : AbstractValidator<CreateReceptionLotDto>
    {
        public CreateReceptionLotValidator()
        {
            RuleFor(x => x.reception_id)
                .GreaterThan(0)
                .WithMessage("La recepción es obligatoria.");

            RuleFor(x => x.product_id)
                .GreaterThan(0)
                .WithMessage("El producto es obligatorio.");

            RuleFor(x => x.lot_number)
                .NotEmpty()
                .WithMessage("El número de lote es obligatorio.")
                .MaximumLength(100);

            RuleFor(x => x.lot_size)
                .GreaterThan(0)
                .When(x => x.lot_size.HasValue)
                .WithMessage("El tamaño del lote debe ser mayor que cero.");

            RuleFor(x => x.unit_count)
                .GreaterThan(0)
                .When(x => x.unit_count.HasValue)
                .WithMessage("La cantidad de unidades debe ser mayor que cero.");

            RuleFor(x => x)
                .Must(x => x.lot_size.HasValue || x.unit_count.HasValue)
                .WithMessage("Debe ingresar el tamaño del lote o la cantidad de unidades.");

            RuleFor(x => x.lot_status)
                .Must(value => string.IsNullOrWhiteSpace(value) || value == "PENDING_APPROVAL" || value == "REJECTED")
                .WithMessage("El estado solo puede ser PENDING_APPROVAL o REJECTED.");

            RuleFor(x => x.document_type)
                .MaximumLength(30);

            RuleFor(x => x.document_number)
                .MaximumLength(100);

            RuleFor(x => x.quantity)
                .GreaterThan(0)
                .When(x => x.quantity.HasValue)
                .WithMessage("La cantidad afectada debe ser mayor que cero.");
        }
    }
}