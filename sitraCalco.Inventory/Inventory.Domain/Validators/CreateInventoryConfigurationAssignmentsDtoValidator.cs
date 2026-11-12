using FluentValidation;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Validators
{
    public class CreateInventoryConfigurationAssignmentsDtoValidator
        : AbstractValidator<CreateInventoryConfigurationAssignmentsDto>
    {
        public CreateInventoryConfigurationAssignmentsDtoValidator()
        {
            RuleFor(x => x.Assignments)
                .NotNull()
                .WithMessage(
                    "Las asignaciones son obligatorias.")
                .NotEmpty()
                .WithMessage(
                    "Debe enviar al menos una asignación.");

            RuleForEach(x => x.Assignments)
                .ChildRules(assignment =>
                {
                    assignment
                        .RuleFor(x => x.SolutionCenterId)
                        .GreaterThan(0)
                        .WithMessage(
                            "El identificador de la bodega o punto de venta debe ser mayor a cero.");

                    assignment
                        .RuleFor(x => x.SectionId)
                        .GreaterThan(0)
                        .WithMessage(
                            "El identificador de la sección debe ser mayor a cero.");
                });

            RuleFor(x => x.Assignments)
                .Must(HaveUniqueAssignments)
                .When(x =>
                    x.Assignments is not null &&
                    x.Assignments.Count > 0)
                .WithMessage(
                    "No se pueden enviar combinaciones repetidas de bodega o punto de venta y sección.");
        }

        private static bool HaveUniqueAssignments(
            List<CreateInventoryConfigurationAssignmentDto> assignments)
        {
            return assignments
                .GroupBy(assignment => new
                {
                    assignment.SolutionCenterId,
                    assignment.SectionId
                })
                .All(group => group.Count() == 1);
        }
    }
}