using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Domain.Interfaces
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<Page<Supplier>> GetAllSupplier(int page, int take);
        Task<IEnumerable<Supplier>> GetOptions(string? search = null, int take = 20);
        Task<Supplier?> GetBySupplierId(long supplierId);
        Task<bool> ExistsByCode(string supplierCode, long? excludeSupplierId = null);
    }
}