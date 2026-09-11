using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly SitraCalcoContext _context;

        public LogRepository(
            SitraCalcoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registra una acción realizada dentro
        /// del módulo de inventarios.
        /// </summary>
        public async Task<long> CreateLog(
            InventoryLog log)
        {
            await _context.InventoryLogs
                .AddAsync(log);

            await _context.SaveChangesAsync();

            return log.log_id;
        }

        public async Task<PagedDto<InventoryLog>> GetPaged(
    DateTime? from,
    DateTime? to,
    int page,
    int take)
        {
            if (page < 1)
                page = 1;

            if (take < 1)
                take = 10;

            var query = _context.InventoryLogs
                .AsNoTracking()
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(log =>
                    log.created_at >= from.Value);
            }

            if (to.HasValue)
            {
                var endDate = to.Value.Date.AddDays(1);

                query = query.Where(log =>
                    log.created_at < endDate);
            }

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(log => log.created_at)
                .ThenByDescending(log => log.log_id)
                .Skip((page - 1) * take)
                .Take(take)
                .ToListAsync();

            var pages = total == 0
                ? 0
                : (int)Math.Ceiling(
                    total / (double)take);

            return new PagedDto<InventoryLog>
            {
                Items = logs,
                Total = total,
                Page = page,
                Take = take,
                Pages = pages
            };
        }
    }
}