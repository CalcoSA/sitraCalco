using Authentication.Domain.Dtos;

namespace Authentication.Application.Interfaces
{
    public interface IJwtTokenApplication
    {
        (string Token, DateTime ExpiresAt) GenerateToken(UserDto user);
    }
}