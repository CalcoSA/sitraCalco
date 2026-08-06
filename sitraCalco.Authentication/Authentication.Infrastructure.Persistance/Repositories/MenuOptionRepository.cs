using Authentication.Infrastructure.Persistance.Data;
using Authentication.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Authentication.Domain.Models;

namespace Authentication.Infrastructure.Persistance.Repositories
{
    public class MenuOptionRepository : Repository<Menuoption>, IMenuOptionRepository
    {
        private readonly SitraCalcoContext _context;
        public MenuOptionRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Consulta que obtine las opciones de menú por un IdRole
        /// </summary>
        /// <param name="idRole">Identificador del registro a obtener</param>
        /// <returns>La información del registro solicitado</returns>
        public async Task<IEnumerable<Menuoption>> GetByRole(int idRole)
        {
            return await _context.Rolemenuoptions
                .AsNoTracking()
                .Where(x =>
                    x.IdRole == idRole &&
                    x.IdMenuOptionNavigation.StatusMenuOption == 1)
                .Select(x => x.IdMenuOptionNavigation)
                .OrderBy(x => x.OrderMenuOption)
                .ToListAsync();
        }
    }
}