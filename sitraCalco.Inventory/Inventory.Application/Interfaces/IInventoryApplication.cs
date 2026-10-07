using Inventory.Domain.Dtos;

namespace Inventory.Application.Interfaces
{
    public interface IInventoryApplication
    {
        Task<InventoryCountsResultDto> GetCounts(string inventoryExecutionId, int? countNumber, string role);
        Task<CreateInventoryResultDto> Create(CreateInventoryDto request, string userLogin, string role);
    }
}
