using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface IInventoryExportParameterRepository
    {
        Task<InventoryExportParameter?> GetActiveByConfigurationCode(
            string configurationCode, CancellationToken cancellationToken = default);
    }
}
