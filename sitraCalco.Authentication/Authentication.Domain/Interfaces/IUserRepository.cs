using Authentication.Domain.Models;
using Authentication.Domain.Dtos;

namespace Authentication.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<PagedDto<User>> GetAllUsers(int page, int take, string? search);
        Task<User?> GetUserById(int idUser);
        Task<User?> GetUserByLogin(string userLogin);
        Task<bool> ExistsByUserLogin(string userLogin, int? excludeIdUser = null);
        Task CreateUser(User user, int idRole);
        Task UpdateUser(int idUser, bool statusUser, int idRole);
        Task DeleteUser(int idUser);
    }
}