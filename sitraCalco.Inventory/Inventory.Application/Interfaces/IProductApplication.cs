using Inventory.Domain.Dtos;
using Inventory.Domain.Models;

namespace Inventory.Application.Interfaces
{
    public interface IProductApplication
    {
        Task<ProductSyncResultDto> SyncProducts();

        Task<ProductImageResultDto> UploadImage(long productId, Stream stream, string fileName, string contentType, long length, string userLogin);
        Task<ProductImageResultDto> GetImage(long productId);
        Task<ProductImageResultDto> ReplaceImage(long productId, Stream stream, string fileName, string contentType, long length, string userLogin);
        Task<ProductImageResultDto> DeleteImage(long productId, string userLogin);

        Task<PagedDto<Product>> GetPaged(int page,int take,string? search = null);
        Task<IEnumerable<ProductOptionDto>> SearchProducts(string search,int take = 20);
    }
}
