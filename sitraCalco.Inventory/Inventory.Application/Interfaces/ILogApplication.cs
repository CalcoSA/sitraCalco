using Inventory.Domain.Dtos;

namespace Inventory.Application.Interfaces
{
    public interface ILogApplication
    {
        Task<long> CreateLog(CreateLogDto request);

        Task<PagedDto<LogDto>?> GetPaged(DateTime? from,DateTime? to,int page,int take);
    }
}