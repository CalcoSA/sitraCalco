using Inventory.Domain.Models;

namespace Inventory.Domain.Interfaces
{
    public interface IPermissionRepository
    {
        Task<IEnumerable<Permission>> GetAll();

        Task<Permission?> GetById(long permissionId);

        Task<Permission?> GetByKey(string permissionKey);

        Task<bool> ExistsByKey(string permissionKey, long? excludePermissionId = null);

        Task<long> CreatePermission(Permission permission);

        Task<bool> UpdatePermission(Permission permission);

        Task<bool> DeletePermission(long permissionId);
    }
}
