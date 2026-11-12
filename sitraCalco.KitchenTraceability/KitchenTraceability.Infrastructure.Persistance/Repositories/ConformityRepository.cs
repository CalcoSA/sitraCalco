using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ConformityRepository : Repository<Conformity>, IConformityRepository
    {
        private readonly SitraCalcoContext _context;

        public ConformityRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener el último número generado de una no conformidad.
        /// </summary>
        /// <returns>Type: string - Último número de no conformidad registrado.</returns>
        public async Task<string?> GetLastNonconformityNumber()
        {
            return await _context.Conformities
                .AsNoTracking()
                .Where(x => x.nonconformity_number.StartsWith("LOG-"))
                .OrderByDescending(x => x.nonconformity_id)
                .Select(x => x.nonconformity_number)
                .FirstOrDefaultAsync();
        }
    }
}