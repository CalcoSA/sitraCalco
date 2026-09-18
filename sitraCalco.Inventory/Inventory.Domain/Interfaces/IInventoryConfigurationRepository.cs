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
        Task<int> CreateAssignments(IEnumerable<InventoryConfigurationAssignment> assignments);
        Task<IEnumerable<SolutionCenterListDto>>GetSolutionCentersByType(long solutionCenterTypeId);
        Task<IEnumerable<SectionDto>>GetPointOfSaleOnlySections();
        Task<SolutionCenterSectionsDto?>GetSolutionCenterWithSections(long solutionCenterId);
        Task<SectionSolutionCentersDto?>GetSectionWithPointOfSales(long sectionId);
        Task<SolutionCenterInventoryConfigurationsDto?>GetInventoryConfigurationsBySolutionCenterId(long solutionCenterId);
        Task<InventoryConfigurationByIdDto?>GetInventoryConfigurationById(long inventoryConfigurationId);
        Task<bool> UpdateAssignmentStatus(long inventoryConfigurationId,long solutionCenterId,long sectionId,bool isActive);
        Task<bool> ExistsByName(string inventoryConfigurationName,long excludeInventoryConfigurationId);
        Task<bool> UpdateInventoryConfiguration(long inventoryConfigurationId,string inventoryConfigurationName,DateTime? startDate,DateTime? endDate);
        Task<bool> DayExists(long inventoryConfigurationId,string dayOfWeek);
        Task<int> AddDays(IEnumerable<InventoryConfigurationDay> days);
        Task<bool> DeleteDay(long inventoryConfigurationId,string dayOfWeek);
        Task<bool> DeleteAssignment(long inventoryConfigurationId,long solutionCenterId,long sectionId);
        Task<int> DeleteAssignmentsBySection(long inventoryConfigurationId,long sectionId);


    }
}