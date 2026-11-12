using Authentication.Domain.Dtos;

namespace Authentication.Application.Interfaces
{
    public interface IMenuOptionApplication
    {
        Task<IEnumerable<MenuOptionDto>> GetAll();
        Task<IEnumerable<MenuOptionDto>> GetByRole(int idRole);
    }
}