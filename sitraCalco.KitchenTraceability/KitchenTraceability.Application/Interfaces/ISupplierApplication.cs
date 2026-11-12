using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Interfaces
{
    public interface ISupplierApplication
    {
        Task<Page<SupplierDto>> GetAll(int page, int take);
        Task<IEnumerable<SupplierDto>> GetOptions(string? search = null);
        Task<SupplierDto?> GetById(long id);
        Task<bool> Create(CreateSupplierDto request);
        Task<bool> Update(long id, UpdateSupplierDto request);
        Task<bool> Delete(long id);
    }
}