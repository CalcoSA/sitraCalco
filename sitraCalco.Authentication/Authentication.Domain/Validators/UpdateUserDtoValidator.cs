using Authentication.Domain.Dtos;
using FluentValidation;

namespace Authentication.Domain.Validators
{
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.IdUser)
                .GreaterThan(0)
                .WithMessage("El IdUser debe ser mayor a cero.");

            RuleFor(x => x.IdRole)
                .GreaterThan(0)
                .WithMessage("El IdRole debe ser mayor a cero.");
        }
    }
}