using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface ISiesaRepository
    {
        Task<IEnumerable<Product>> GetProducts();
    }
}