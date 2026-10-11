using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ReceptionVisualInspectionRepository : Repository<ReceptionVisualInspection>, IReceptionVisualInspectionRepository
    {
        private readonly SitraCalcoContext _context;

        public ReceptionVisualInspectionRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener la inspección visual registrada para un lote.
        /// </summary>
        /// <param name="receptionLotId">Type: long - Id del lote.</param>
        /// <returns>Type: ReceptionVisualInspection - Inspección encontrada.</returns>
        public async Task<ReceptionVisualInspection?> GetByReceptionLotId(long receptionLotId)
        {
            return await _context.ReceptionVisualInspections
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.reception_lot_id == receptionLotId);
        }
    }
}