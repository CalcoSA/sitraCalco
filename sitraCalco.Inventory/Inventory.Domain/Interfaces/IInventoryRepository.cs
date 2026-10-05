using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface IInventoryRepository
    {
        Task<InventoryCountsDto?> GetCountsContext(string inventoryExecutionId);
        Task<List<InventoryProductCountsDto>> GetCounts(string inventoryExecutionId, int? countNumber);
        Task<InventorySaveContextDto?> GetContext(long solutionCenterId, long inventoryConfigurationId, long sectionId);
        Task<IEnumerable<Product>> GetProducts(long solutionCenterId, long sectionId, IEnumerable<long> productIds);
        Task<IEnumerable<InventoryExecutionContextDto>> GetExecutionContexts(string inventoryExecutionId);
        Task<bool> HasDuplicates(IReadOnlyCollection<InventoryRecord> records);
        Task<bool> Save(IReadOnlyCollection<InventoryRecord> records, bool isExistingExecution);
    }
}
