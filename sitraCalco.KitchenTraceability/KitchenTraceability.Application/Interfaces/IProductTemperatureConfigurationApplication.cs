using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Interfaces
{
    public interface IProductTemperatureConfigurationApplication
    {
        Task<bool> Create(CreateProductTemperatureConfigurationDto request, string user);
        Task<bool> Update(long id, UpdateProductTemperatureConfigurationDto request, string user);
        Task<bool> Delete(long id);
    }
}