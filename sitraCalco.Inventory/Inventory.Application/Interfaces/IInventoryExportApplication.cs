using Inventory.Domain.Dtos;

namespace Inventory.Application.Interfaces
{
    public interface IInventoryExportApplication
    {
        Task<InventoryExportResultDto> GetSiesa(string? date, IReadOnlyCollection<long>? solutionCenterIds,
            string? role, CancellationToken cancellationToken = default);
    }
}
