using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class PaginationValidator : AbstractValidator<PaginationDto>
    {
        public PaginationValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("La página debe ser mayor a 0.");

            RuleFor(x => x.Take)
                .GreaterThan(0)
                .WithMessage("La cantidad de registros debe ser mayor a 0.")
                .LessThanOrEqualTo(100)
                .WithMessage("La cantidad máxima de registros por página es 100.");
        }
    }
}