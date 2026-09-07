using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Application.Interfaces
{
    public interface ISolutionCenterApplication
    {
        Task<IEnumerable<SolutionCenterType>> GetSolutionCenterTypes();

        Task<long> CreateSolutionCenter(CreateSolutionCenterDto request);

        Task<long> CreateSectionConfiguration(long solutionCenterId,CreateSectionConfigurationDto request);
    }
}