using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IProductReceptionConfigurationRepository : IRepository<ProductReceptionConfiguration>
    {
        Task<ProductReceptionConfiguration?> GetByProductConfigurationId(long configurationId);
        Task<ProductReceptionConfiguration?> GetByProductId(long productId);
    }
}