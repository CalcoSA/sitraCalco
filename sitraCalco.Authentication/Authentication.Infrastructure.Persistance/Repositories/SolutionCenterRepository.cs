using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using Authentication.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistance.Repositories
{
    public class SolutionCenterRepository : ISolutionCenterRepository
    {
        private readonly SitraCalcoContext _context;

        public SolutionCenterRepository(SitraCalcoContext context) => _context = context;

        public async Task<IEnumerable<SolutionCenter>> GetAll()
            => await _context.SolutionCenters.AsNoTracking()
                .OrderBy(x => x.SolutionCenterCode)
                .ThenBy(x => x.SolutionCenterId)
                .ToListAsync();
    }
}
