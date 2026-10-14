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

        Task<AuditResult<long>> CreateSectionConfiguration(long solutionCenterId,Section section,List<SectionProductDto> products,string createdBy);

        Task<bool> SolutionCenterNameExists(string solutionCenterName);

        Task<bool> SectionNameExists(long solutionCenterId,string sectionName);

        Task<PagedDto<SolutionCenterListDto>> GetPagedSolutionCenters(int page,int take,long? solutionCenterTypeId = null);

        Task<SolutionCenterDetailDto?> GetSolutionCenterById(long solutionCenterId);

        Task<bool> UpdateSolutionCenterStatus(long solutionCenterId,bool isActive);

        Task<AuditResult<bool>> UpdateSectionStatus(long sectionId,bool isActive);

        Task<IEnumerable<Product>> GetSectionProducts(long solutionCenterId,long sectionId);

        Task<bool> SectionBelongsToSolutionCenter(long solutionCenterId,long sectionId);

        Task<Section?> GetSectionById(long sectionId);

        Task<SolutionCenterSection?> GetSectionAssignment(long solutionCenterId,long sectionId);

        Task<bool> CreateSectionAssignment(SolutionCenterSection assignment);

        Task<bool> UpdateSectionAssignmentStatus(long solutionCenterId,long sectionId,bool isActive);

        Task<AuditResult<long>> AddProductToSection(long solutionCenterId,long sectionId,Product product,int position,string createdBy);

        Task<AuditResult<bool>> UpdateProductOrder(long solutionCenterId,long sectionId,long solutionCenterProductId,int newPosition);

        Task<AuditResult<bool>> DeleteProductFromSection(long solutionCenterId,long sectionId,long solutionCenterProductId);

        Task<bool> SolutionCenterCodeExists(string solutionCenterCode,long excludeSolutionCenterId);

        Task<bool> SolutionCenterNameExists(string solutionCenterName,long excludeSolutionCenterId);
        Task<bool> UpdateSolutionCenter(long solutionCenterId,string solutionCenterCode,string solutionCenterName);
        Task<bool> SectionNameExists(string sectionName);
    }
}
