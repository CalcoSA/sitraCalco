using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Page<Product>> GetAllProduct(int page, int take, string? search = null);
        Task<IEnumerable<Product>> GetOptions(string? search = null, int take = 20);
        Task<Product?> GetProductById(long productId);
        Task AddRange(IEnumerable<Product> products);
    }
}