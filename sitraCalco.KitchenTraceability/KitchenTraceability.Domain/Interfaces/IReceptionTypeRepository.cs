using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IReceptionTypeRepository : IRepository<ReceptionType>
    {
        Task<Page<ReceptionType>> GetPaged(int page, int take, string? search = null);
        Task<IEnumerable<ReceptionType>> GetOptions(string? search = null);
        Task<ReceptionType?> GetReceptionTypeById(long receptionTypeId);
    }
}