using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ReceptionLotRepository : Repository<ReceptionLot>, IReceptionLotRepository
    {
        private readonly SitraCalcoContext _context;

        public ReceptionLotRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener un registro de la tabla reception_lot por Id.
        /// </summary>
        /// <param name="receptionLotId">Type: long - Id del lote de recepción.</param>
        /// <returns>Type: ReceptionLot - Lote encontrado.</returns>
        public async Task<ReceptionLot?> GetByReceptionLotId(long receptionLotId)
        {
            return await _context.ReceptionLots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.reception_lot_id == receptionLotId);
        }
    }
}