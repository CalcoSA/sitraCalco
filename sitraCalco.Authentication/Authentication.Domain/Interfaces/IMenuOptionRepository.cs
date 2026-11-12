using Authentication.Domain.Models;

namespace Authentication.Domain.Interfaces
{
    public interface IMenuOptionRepository : IRepository<Menuoption>
    {
        Task<IEnumerable<Menuoption>> GetByRole(int idRole);
    }
}