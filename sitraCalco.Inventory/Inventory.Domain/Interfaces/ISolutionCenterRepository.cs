using Inventory.Domain.Models;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Interfaces
{
    public interface ISolutionCenterRepository
    {
        Task<IEnumerable<SolutionCenterType>>GetSolutionCenterTypes();

        Task<bool> SolutionCenterTypeExists(long solutionCenterTypeId);

        Task<bool> SolutionCenterCodeExists(string solutionCenterCode);

        Task<long> CreateSolutionCenter(SolutionCenter solutionCenter);

        Task<bool> SolutionCenterExists(long solutionCenterId);

        Task<long> CreateSectionConfiguration(long solutionCenterId,Section section,IEnumerable<SectionProductDto> products,string createdBy);

        Task<bool> SolutionCenterNameExists(string solutionCenterName);

        Task<bool> SectionNameExists(long solutionCenterId,string sectionName);
    }
}