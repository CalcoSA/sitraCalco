using KitchenTraceability.Infrastructure.Persistance.Extensions;
using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ReceptionTypeRepository : Repository<ReceptionType>, IReceptionTypeRepository
    {
        private readonly SitraCalcoContext _context;

        public ReceptionTypeRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener los tipos de recepción de la tabla reception_type.
        /// </summary>
        /// <param name="page">Type: int - Número de página.</param>
        /// <param name="take">Type: int - Cantidad de registros por página.</param>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o código de formulario.</param>
        /// <returns>Type: Page - Lista paginada de tipos de recepción.</returns>
        public async Task<Page<ReceptionType>> GetPaged(int page, int take, string? search = null)
        {
            var query = _context.ReceptionTypes
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(receptionType =>
                    receptionType.name.Contains(search) || (receptionType.form_code != null && receptionType.form_code.Contains(search)));
            }

            query = query.OrderBy(receptionType => receptionType.reception_type_id);

            return await query.ToPagedAsync(page, take);
        }

        /// <summary>
        /// Método para obtener los tipos de recepción activos para listas desplegables.
        /// </summary>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o código de formulario.</param>
        /// <returns>Type: IEnumerable - Lista de tipos de recepción activos.</returns>
        public async Task<IEnumerable<ReceptionType>> GetOptions(string? search = null)
        {
            var query = _context.ReceptionTypes
                .AsNoTracking()
                .Where(receptionType => receptionType.is_active == true);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(receptionType =>
                    receptionType.name.Contains(search) || (receptionType.form_code != null && receptionType.form_code.Contains(search)));
            }

            return await query
                .OrderBy(receptionType => receptionType.name)
                .ThenBy(receptionType => receptionType.form_code)
                .ToListAsync();
        }

        /// <summary>
        /// Método para obtener un tipo de recepción por Id de la tabla reception_type.
        /// </summary>
        /// <param name="receptionTypeId">Type: long - Id del tipo de recepción.</param>
        /// <returns>Type: ReceptionType - Tipo de recepción encontrado.</returns>
        public async Task<ReceptionType?> GetReceptionTypeById(long receptionTypeId)
        {
            return await _context.ReceptionTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(receptionType => receptionType.reception_type_id == receptionTypeId);
        }
    }
}