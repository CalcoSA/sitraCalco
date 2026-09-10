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

        Task<PagedDto<SolutionCenterListDto>> GetPagedSolutionCenters(int page,int take,long? solutionCenterTypeId = null);

        Task<SolutionCenterDetailDto?> GetSolutionCenterById(long solutionCenterId);

        Task<bool> UpdateSolutionCenterStatus(long solutionCenterId,bool isActive);

        Task<bool> UpdateSectionStatus(long sectionId,bool isActive);

        Task<IEnumerable<Product>> GetSectionProducts(long solutionCenterId,long sectionId);

        Task<bool> SectionBelongsToSolutionCenter(long solutionCenterId,long sectionId);

        Task<long> AddProductToSection(long solutionCenterId,long sectionId,long productId,int position,string createdBy);

        Task<bool> UpdateProductOrder(long solutionCenterId,long sectionId,long solutionCenterProductId,int newPosition);

        Task<bool> DeleteProductFromSection(long solutionCenterId,long sectionId,long solutionCenterProductId);

        Task<bool> SolutionCenterCodeExists(string solutionCenterCode,long excludeSolutionCenterId);

        Task<bool> SolutionCenterNameExists(string solutionCenterName,long excludeSolutionCenterId);
        Task<bool> UpdateSolutionCenter(long solutionCenterId,string solutionCenterCode,string solutionCenterName);
    }
}