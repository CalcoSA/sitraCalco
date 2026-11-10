using Inventory.Domain.Models;
using Inventory.Domain.Dtos;

namespace Inventory.Domain.Interfaces
{
    public interface ISiesaRepository
    {
        Task<IEnumerable<Product>> GetProducts();
        Task<IReadOnlyDictionary<string, SiesaExistenceDto>> GetExistences(string warehouseCode, IReadOnlyCollection<string> references);
    }
}