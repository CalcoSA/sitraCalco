using Inventory.Domain.Dtos;

namespace Inventory.Application.Interfaces
{
    public interface IInventoryApplication
    {
        Task<InventoryCountsResultDto> GetCounts(string inventoryExecutionId, int? countNumber, string role, string? solutionCenterCode);
        Task<CreateInventoryResultDto> Create(CreateInventoryDto request, string userLogin, string role, string? solutionCenterCode);
        Task<(InventoryCountsStatus Status, string? ExecutionId, string? Message)> GetTodayExecution(long solutionCenterId, long inventoryConfigurationId, long sectionId, string role, string? solutionCenterCode);
    }
}