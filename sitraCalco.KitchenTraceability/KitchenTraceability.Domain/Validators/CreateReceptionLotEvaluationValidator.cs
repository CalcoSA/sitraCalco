using KitchenTraceability.Domain.Dtos;
using FluentValidation;

namespace KitchenTraceability.Domain.Validators
{
    public class CreateReceptionLotEvaluationValidator : AbstractValidator<CreateReceptionLotEvaluationDto>
    {
        public CreateReceptionLotEvaluationValidator()
        {
            RuleFor(x => x.reception_lot_id)
                .GreaterThan(0)
                .WithMessage("El lote de recepción es obligatorio.");

            RuleForEach(x => x.temperature_measurements)
                .ChildRules(measurement =>
                {
                    measurement.RuleFor(x => x.measurement_point)
                        .NotEmpty()
                        .WithMessage("El punto de medición es obligatorio.")
                        .Must(value => value == "START" || value == "MIDDLE" || value == "END")
                        .WithMessage("El punto de medición solo puede ser START, MIDDLE o END.");
                });

            RuleFor(x => x.temperature_measurements)
                .Must(measurements =>
                {
                    if (measurements is null)
                    {
                        return true;
                    }

                    var list = measurements.ToList();

                    if (list.Count == 0)
                    {
                        return true;
                    }

                    if (list.Count != 3)
                    {
                        return false;
                    }

                    return list
                        .Select(x => x.measurement_point)
                        .Distinct()
                        .Count() == 3;
                })
                .WithMessage("Las mediciones deben contener una sola vez START, MIDDLE y END.");

            RuleFor(x => x.visual_inspection!.inspected_units)
                .GreaterThan(0)
                .When(x => x.visual_inspection is not null && x.visual_inspection.inspected_units.HasValue)
                .WithMessage("Las unidades inspeccionadas deben ser mayores que cero.");

            RuleFor(x => x.visual_inspection!.compliant_units)
                .GreaterThanOrEqualTo(0)
                .When(x => x.visual_inspection is not null)
                .WithMessage("Las unidades conformes no pueden ser negativas.");

            RuleFor(x => x.visual_inspection!.non_compliant_units)
                .GreaterThanOrEqualTo(0)
                .When(x => x.visual_inspection is not null)
                .WithMessage("Las unidades no conformes no pueden ser negativas.");

            RuleFor(x => x.visual_inspection!.comments)
                .NotEmpty()
                .When(x => x.visual_inspection is not null && x.visual_inspection.non_compliant_units > 0)
                .WithMessage("Debe ingresar una observación cuando existen unidades no conformes.");

            RuleFor(x => x.document_type)
                .MaximumLength(30);

            RuleFor(x => x.document_number)
                .MaximumLength(100);
        }
    }
}