using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IReceptionRepository : IRepository<Reception>
    {
        Task<Reception?> GetByReceptionId(long receptionId);
        Task<Reception> CreateReception(Reception reception);
    }
}