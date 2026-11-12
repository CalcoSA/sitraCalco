using Inventory.Domain.Dtos;

namespace Inventory.Domain.Interfaces
{
    public interface IInventoryExportRepository
    {
        Task<List<InventoryExportCenterAccessDto>> GetCenters(
            IReadOnlyCollection<long> centerIds, DateTime start, DateTime end, CancellationToken cancellationToken = default);
        Task<InventoryExportReadDto> Read(
            DateTime start, DateTime end, IReadOnlyCollection<long> centerIds, bool warehousesOnly,
            CancellationToken cancellationToken = default);
    }
}
