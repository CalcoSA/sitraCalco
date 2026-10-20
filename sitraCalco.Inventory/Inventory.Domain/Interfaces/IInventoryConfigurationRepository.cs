using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface IInventoryConfigurationRepository
    {
        Task<bool> ExistsByName(string inventoryConfigurationName);
        Task<long> CreateInventoryConfiguration(InventoryConfiguration inventoryConfiguration);
        Task<bool> ConfigurationExists(long inventoryConfigurationId);
        Task<bool> SolutionCenterExists(long solutionCenterId);
        Task<bool> SectionExists(long sectionId);
        Task<bool> SectionBelongsToSolutionCenter(long solutionCenterId,long sectionId);
        Task<bool> AssignmentExists(long inventoryConfigurationId,long solutionCenterId,long sectionId);
        Task<AuditResult<int>> CreateAssignments(IEnumerable<InventoryConfigurationAssignment> assignments);
        Task<IEnumerable<SolutionCenterListDto>>GetSolutionCentersByType(long solutionCenterTypeId);
        Task<IEnumerable<SectionDto>>GetPointOfSaleOnlySections();
        Task<SolutionCenterSectionsDto?>GetSolutionCenterWithSections(long solutionCenterId);
        Task<SectionSolutionCentersDto?>GetSectionWithPointOfSales(long sectionId);
        Task<SolutionCenter?> GetSolutionCenterById(long solutionCenterId);
        Task<IEnumerable<InventoryConfigurationAvailabilityDto>> GetAvailabilityCandidates(long solutionCenterId);
        Task<IEnumerable<AvailableInventorySectionDto>> GetAvailableInventorySections(long solutionCenterId, long inventoryConfigurationId);
        Task<PagedDto<AvailableInventoryProductDto>> GetAvailableInventoryProducts(long solutionCenterId, long sectionId, int page, int take);
        Task<SolutionCenterInventoryConfigurationsDto?>GetInventoryConfigurationsBySolutionCenterId(long solutionCenterId);
        Task<InventoryConfigurationByIdDto?>GetInventoryConfigurationById(long inventoryConfigurationId);
        Task<AuditResult<bool>> UpdateAssignmentStatus(long inventoryConfigurationId,long solutionCenterId,long sectionId,bool isActive);
        Task<bool> ExistsByName(string inventoryConfigurationName,long excludeInventoryConfigurationId);
        Task<InventoryConfigurationDto?> UpdateInventoryConfiguration(long inventoryConfigurationId, UpdateInventoryConfigurationDto request);
        Task<PagedDto<InventoryConfigurationListDto>> GetInventoryConfigurations(int page, int take);
        Task<bool> DayExists(long inventoryConfigurationId,string dayOfWeek);
        Task<AuditResult<int>> AddDays(IEnumerable<InventoryConfigurationDay> days);
        Task<AuditResult<bool>> DeleteDay(long inventoryConfigurationId,string dayOfWeek);
        Task<AuditResult<bool>> DeleteAssignment(long inventoryConfigurationId,long solutionCenterId,long sectionId);
        Task<AuditResult<int>> DeleteAssignmentsBySection(long inventoryConfigurationId,long sectionId);


    }
}
