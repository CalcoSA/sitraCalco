using Authentication.Domain.Dtos;

namespace Authentication.Application.Interfaces
{
    public interface IWordpressUserApplication
    {
        Task<WordpressUserDto?> GetByLogin(string userLogin);
    }
}