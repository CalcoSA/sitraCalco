using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface ISiesaRepository
    {
        Task<IEnumerable<Product>> GetProducts();
    }
}