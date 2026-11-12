using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Interfaces
{
    public interface IProductApplication
    {
        Task<Page<ProductDto>> GetAll(int page, int take, string? search = null);
        Task<IEnumerable<ProductDto>> GetOptions(string? search = null);
        Task<ProductDetailDto?> GetById(long id);
        Task<ProductResultDto> GetSiesaProducts();
    }
}