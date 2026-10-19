using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Application.Interfaces
{
    public interface ISolutionCenterApplication
    {
        Task<IEnumerable<SolutionCenterType>> GetSolutionCenterTypes();

        Task<long> CreateSolutionCenter(CreateSolutionCenterDto request);

        Task<AuditResult<long>> CreateSectionConfiguration(long solutionCenterId,CreateSectionConfigurationDto request,string userName);

        Task<PagedDto<SolutionCenterListDto>?> GetPagedSolutionCenters(string role,int page,int take);

        Task<SolutionCenterDetailDto?> GetSolutionCenterById(long solutionCenterId);

        Task<bool> UpdateSolutionCenterStatus(long solutionCenterId,bool isActive);

        Task<AuditResult<bool>> UpdateSectionStatus(long sectionId,bool isActive);

        Task<SolutionCenterSectionResultDto> AssignExistingSection(long solutionCenterId,long sectionId);

        Task<SolutionCenterSectionResultDto> UpdateSectionAssignmentStatus(long solutionCenterId,long sectionId,bool isActive);

        Task<AuditResult<SolutionCenterSectionResultDto>> DeleteSectionAssignment(long solutionCenterId,long sectionId);

        Task<AuditResult<long>> AddProductToSection(long solutionCenterId,long sectionId,AddSectionProductDto request,string userName);

        Task<AuditResult<bool>> UpdateProductOrder(long solutionCenterId,long sectionId,long solutionCenterProductId,int newPosition);

        Task<AuditResult<bool>> DeleteProductFromSection(long solutionCenterId,long sectionId,long solutionCenterProductId);
        Task<bool> UpdateSolutionCenter(long solutionCenterId,UpdateSolutionCenterDto request);
    }
}
