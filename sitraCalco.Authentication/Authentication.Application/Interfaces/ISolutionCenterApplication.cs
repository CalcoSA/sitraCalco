using Authentication.Domain.Dtos;

namespace Authentication.Application.Interfaces
{
    public interface ISolutionCenterApplication
    {
        Task<IEnumerable<SolutionCenterDto>> GetAll();
    }
}
