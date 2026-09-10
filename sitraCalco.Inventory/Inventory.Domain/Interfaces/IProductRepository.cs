using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<ProductSyncResultDto> UpsertRange(IEnumerable<Product> products);

        Task<PagedDto<Product>> GetPaged(int page,int take,string? search = null);

        Task<IEnumerable<Product>> SearchProducts(string search,int take = 20);

        Task<IEnumerable<Product>> GetByIds(IEnumerable<long> productIds);

        Task<Product?> GetProductById(long productId);


    }
}