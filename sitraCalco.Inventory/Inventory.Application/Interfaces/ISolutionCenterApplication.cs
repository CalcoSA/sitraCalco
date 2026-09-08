using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Application.Interfaces
{
    public interface ISolutionCenterApplication
    {
        Task<IEnumerable<SolutionCenterType>> GetSolutionCenterTypes();

        Task<long> CreateSolutionCenter(CreateSolutionCenterDto request);

        Task<long> CreateSectionConfiguration(long solutionCenterId,CreateSectionConfigurationDto request);

        Task<PagedDto<SolutionCenterListDto>?> GetPagedSolutionCenters(string role,int page,int take);

        Task<SolutionCenterDetailDto?> GetSolutionCenterById(long solutionCenterId);

        Task<bool> UpdateSolutionCenterStatus(long solutionCenterId,bool isActive);

        Task<bool> UpdateSectionStatus(long sectionId,bool isActive);

        Task<long> AddProductToSection(long solutionCenterId,long sectionId,AddSectionProductDto request);

        Task<bool> UpdateProductOrder(long solutionCenterId,long sectionId,long solutionCenterProductId,int newPosition);

        Task<bool> DeleteProductFromSection(long solutionCenterId,long sectionId,long solutionCenterProductId);
    }
}