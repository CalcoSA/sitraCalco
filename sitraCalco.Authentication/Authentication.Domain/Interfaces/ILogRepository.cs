using Authentication.Domain.Dtos;
using Authentication.Domain.Models;

namespace Authentication.Domain.Interfaces
{
    public interface ILogRepository
    {
        Task<long> CreateLog(AuthenticationLog log);

        Task<PagedDto<AuthenticationLog>> GetPaged(DateTime? from, DateTime? to, int page, int take);
    }
}
