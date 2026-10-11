using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IConformityRepository : IRepository<Conformity>
    {
        Task<string?> GetLastNonconformityNumber();
    }
}