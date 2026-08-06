using Authentication.Domain.Dtos;

namespace Authentication.Application.Interfaces
{
    public interface IAuthApplication
    {
        Task<AuthUserDto?> Login(LoginDto loginData);
        Task<AuthUserDto?> IntranetAccess(IntranetAccessDto accessData);
    }
}