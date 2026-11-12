using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Interfaces
{
    public interface IProductReceptionConfigurationApplication
    {
        Task<bool> Create(CreateProductReceptionConfigurationDto request, string user);
        Task<bool> Update(long id, UpdateProductReceptionConfigurationDto request, string user);
        Task<bool> Delete(long id);
    }
}