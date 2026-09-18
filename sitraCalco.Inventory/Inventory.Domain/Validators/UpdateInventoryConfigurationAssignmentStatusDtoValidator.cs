using FluentValidation;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Validators
{
    public class UpdateInventoryConfigurationAssignmentStatusDtoValidator
        : AbstractValidator<UpdateInventoryConfigurationAssignmentStatusDto>
    {
        public UpdateInventoryConfigurationAssignmentStatusDtoValidator()
        {
            RuleFor(x => x.IsActive)
                .NotNull()
                .WithMessage(
                    "El estado de la asignación es obligatorio.");
        }
    }
}