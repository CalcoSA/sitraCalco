using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface IProductTemperatureConfigurationRepository : IRepository<ProductTemperatureConfiguration>
    {
        Task<ProductTemperatureConfiguration?> GetByTemperatureConfigurationId(long configurationId);
        Task<ProductTemperatureConfiguration?> GetByProductId(long productId);
    }
}