using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class InventoryExportParameterRepository : IInventoryExportParameterRepository
    {
        private readonly SitraCalcoContext _context;

        public InventoryExportParameterRepository(SitraCalcoContext context)
        {
            _context = context;
        }

        public Task<InventoryExportParameter?> GetActiveByConfigurationCode(
            string configurationCode, CancellationToken cancellationToken = default)
        {
            return _context.InventoryExportParameters.AsNoTracking()
                .SingleOrDefaultAsync(parameter => parameter.configuration_code == configurationCode &&
                    parameter.is_active, cancellationToken);
        }
    }
}
