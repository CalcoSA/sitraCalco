using Inventory.Domain.Dtos;

namespace Inventory.Application.Interfaces
{
    public interface IInventoryConfigurationApplication
    {
        Task<long> Create(CreateInventoryConfigurationDto request);
        Task<int> CreateAssignments(long inventoryConfigurationId,CreateInventoryConfigurationAssignmentsDto request);
        Task<object?> GetOptions(int type);
        Task<SolutionCenterSectionsDto?> GetSolutionCenterWithSections(long solutionCenterId);
        Task<SectionSolutionCentersDto?>GetSectionWithPointOfSales(long sectionId);
        Task<SolutionCenterInventoryConfigurationsResultDto>GetInventoryConfigurationsBySolutionCenterId(long solutionCenterId,string role);
        Task<InventoryConfigurationByIdResultDto>GetInventoryConfigurationById(long inventoryConfigurationId,string role);
        Task<bool> UpdateAssignmentStatus(long inventoryConfigurationId,long solutionCenterId,long sectionId,bool isActive);
        Task<bool> UpdateInventoryConfiguration(long inventoryConfigurationId,UpdateInventoryConfigurationDto request);
        Task<int> AddDays(long inventoryConfigurationId, AddInventoryConfigurationDaysDto request);
        Task<bool> DeleteDay(long inventoryConfigurationId,string dayOfWeek);
        Task<bool> DeleteAssignment(long inventoryConfigurationId,long solutionCenterId,long sectionId);
        Task<int> DeleteAssignmentsBySection(long inventoryConfigurationId,long sectionId);

    }
}