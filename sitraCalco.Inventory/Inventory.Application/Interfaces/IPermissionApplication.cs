using Inventory.Domain.Dtos;

namespace Inventory.Application.Interfaces
{
    public interface IPermissionApplication
    {
        Task<IEnumerable<PermissionDto>> GetAll();

        Task<PermissionDto?> GetById(long permissionId);

        Task<PermissionDto?> GetByKey(string permissionKey);

        Task<long> Create(CreatePermissionDto request);

        Task<bool> Update(long permissionId, UpdatePermissionDto request);

        Task<bool> Delete(long permissionId);

        Task<bool> HasPermission(string permissionKey, string role);

        Task<IEnumerable<PermissionDto>> GetPermissionsByRole(string role);
    }
}
