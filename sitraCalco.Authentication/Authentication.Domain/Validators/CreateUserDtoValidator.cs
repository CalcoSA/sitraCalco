using Authentication.Domain.Dtos;
using FluentValidation;

namespace Authentication.Domain.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.WordpressUserId)
                .GreaterThan(0UL)
                .WithMessage("El WordpressUserId debe ser mayor a cero.");

            RuleFor(x => x.UserLogin)
                .NotEmpty()
                .WithMessage("El UserLogin es obligatorio.")
                .MaximumLength(60)
                .WithMessage("El UserLogin no puede superar 60 caracteres.");

            RuleFor(x => x.UserName)
                .NotEmpty()
                .WithMessage("El UserName es obligatorio.")
                .MaximumLength(250)
                .WithMessage("El UserName no puede superar 250 caracteres.");

            RuleFor(x => x.IdRole)
                .GreaterThan(0)
                .WithMessage("El IdRole debe ser mayor a cero.");
        }
    }
}