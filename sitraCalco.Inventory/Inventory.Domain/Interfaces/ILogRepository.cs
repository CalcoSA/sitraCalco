using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface ILogRepository
    {
        Task<long> CreateLog(InventoryLog log);

        Task<PagedDto<InventoryLog>> GetPaged(DateTime? from,DateTime? to,int page,int take);
    }
}