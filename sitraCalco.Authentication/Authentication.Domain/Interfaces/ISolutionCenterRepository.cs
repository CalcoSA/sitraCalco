using Authentication.Domain.Models;

namespace Authentication.Domain.Interfaces
{
    public interface ISolutionCenterRepository
    {
        Task<IEnumerable<SolutionCenter>> GetAll();
    }
}
