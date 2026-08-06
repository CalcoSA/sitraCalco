using Authentication.Domain.Dtos;

namespace Authentication.Domain.Interfaces
{
    public interface IWordpressUserRepository
    {
        Task<WordpressUserDto?> GetByLogin(string userLogin);
        bool Verify(string password, string wordpressHash);
    }
}