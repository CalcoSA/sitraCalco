using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ReceptionTemperatureMeasurementRepository : Repository<ReceptionTemperatureMeasurement>, IReceptionTemperatureMeasurementRepository
    {
        private readonly SitraCalcoContext _context;

        public ReceptionTemperatureMeasurementRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener las mediciones de temperatura de un lote.
        /// </summary>
        /// <param name="receptionLotId">Type: long - Id del lote.</param>
        /// <returns>Type: IEnumerable - Mediciones registradas.</returns>
        public async Task<IEnumerable<ReceptionTemperatureMeasurement>> GetByReceptionLotId(long receptionLotId)
        {
            return await _context.ReceptionTemperatureMeasurements
                .AsNoTracking()
                .Where(x => x.reception_lot_id == receptionLotId)
                .ToListAsync();
        }
    }
}