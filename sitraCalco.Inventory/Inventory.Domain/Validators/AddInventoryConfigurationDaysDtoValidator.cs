using FluentValidation;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Validators
{
    public class AddInventoryConfigurationDaysDtoValidator
        : AbstractValidator<AddInventoryConfigurationDaysDto>
    {
        private static readonly string[] ValidDays =
        {
            "Lunes",
            "Martes",
            "Miercoles",
            "Jueves",
            "Viernes",
            "Sabado",
            "Domingo"
        };

        public AddInventoryConfigurationDaysDtoValidator()
        {
            RuleFor(x => x.Days)
                .NotNull()
                .WithMessage(
                    "Los días son obligatorios.")
                .NotEmpty()
                .WithMessage(
                    "Debe enviar al menos un día.");

            RuleForEach(x => x.Days)
                .NotEmpty()
                .WithMessage(
                    "El día no puede estar vacío.")
                .Must(day =>
                    ValidDays.Contains(
                        day.Trim(),
                        StringComparer.OrdinalIgnoreCase))
                .WithMessage(
                    "El día enviado no es válido.");

            RuleFor(x => x.Days)
                .Must(HaveUniqueDays)
                .When(x =>
                    x.Days is not null &&
                    x.Days.Count > 0)
                .WithMessage(
                    "No se pueden enviar días repetidos.");
        }

        private static bool HaveUniqueDays(
            List<string> days)
        {
            return days
                .Select(day =>
                    day.Trim().ToUpperInvariant())
                .Distinct()
                .Count() ==
                days.Count;
        }
    }
}