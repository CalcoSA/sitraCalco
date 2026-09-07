using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Application.Interfaces
{
    public interface IProductApplication
    {
        Task<int> SyncProducts();

        Task<PagedDto<Product>> GetPaged(int page,int take,string? search = null);
        Task<IEnumerable<ProductOptionDto>> SearchProducts(string search,int take = 20);
    }
}