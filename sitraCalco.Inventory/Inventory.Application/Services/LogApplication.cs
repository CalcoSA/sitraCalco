using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;

namespace Inventory.Application.Services
{
    public class LogApplication : ILogApplication
    {
        private readonly ILogRepository _logRepository;

        public LogApplication(
            ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        /// <summary>
        /// Registra una acción realizada dentro
        /// del módulo de inventarios.
        /// </summary>
        public async Task<long> CreateLog(
            CreateLogDto request)
        {
            try
            {
                if (request is null)
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.Action))
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.Module))
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.Description))
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.UserName))
                    return 0;

                var log = new InventoryLog
                {
                    action =
                        request.Action.Trim(),

                    module =
                        request.Module.Trim(),

                    description =
                        request.Description.Trim(),

                    user_name =
                        request.UserName.Trim()
                };

                return await _logRepository
                    .CreateLog(log);
            }
            catch
            {
                throw;
            }
        }
        public async Task<PagedDto<LogDto>?> GetPaged(
    DateTime? from,
    DateTime? to,
    int page,
    int take)
        {
            try
            {
                if (page <= 0)
                    page = 1;

                if (take <= 0)
                    take = 10;

                if (take > 100)
                    take = 100;

                if (from.HasValue &&
                    to.HasValue &&
                    from.Value.Date > to.Value.Date)
                {
                    return null;
                }

                var logs = await _logRepository
                    .GetPaged(
                        from,
                        to,
                        page,
                        take);

                return new PagedDto<LogDto>
                {
                    Items = logs.Items
                        .Select(log => new LogDto
                        {
                            LogId = log.log_id,
                            Action = log.action,
                            Module = log.module,
                            Description = log.description,
                            UserName = log.user_name,
                            CreatedAt = log.created_at
                        })
                        .ToList(),

                    Total = logs.Total,
                    Page = logs.Page,
                    Take = logs.Take,
                    Pages = logs.Pages
                };
            }
            catch
            {
                throw;
            }
        }
    }
}