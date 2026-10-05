using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Helpers;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;

namespace Inventory.Application.Services
{
    public class PermissionApplication : IPermissionApplication
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionApplication(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<IEnumerable<PermissionDto>> GetAll()
        {
            var permissions = await _permissionRepository.GetAll();
            return permissions.Select(MapPermission).ToList();
        }

        public async Task<PermissionDto?> GetById(long permissionId)
        {
            if (permissionId <= 0)
                return null;

            var permission = await _permissionRepository.GetById(permissionId);
            return permission is null ? null : MapPermission(permission);
        }

        public async Task<PermissionDto?> GetByKey(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
                return null;

            var permission = await _permissionRepository.GetByKey(
                PermissionValues.NormalizeKey(permissionKey));
            return permission is null ? null : MapPermission(permission);
        }

        public async Task<long> Create(CreatePermissionDto request)
        {
            if (request is null || !IsValidInput(request.PermissionKey, request.PermissionValue))
                return 0;

            var key = PermissionValues.NormalizeKey(request.PermissionKey);
            if (await _permissionRepository.ExistsByKey(key))
                return 0;

            return await _permissionRepository.CreatePermission(new Permission
            {
                permission_key = key,
                permission_value = PermissionValues.NormalizeRoles(request.PermissionValue)
            });
        }

        public async Task<bool> Update(long permissionId, UpdatePermissionDto request)
        {
            if (permissionId <= 0 || request is null ||
                !IsValidInput(request.PermissionKey, request.PermissionValue))
                return false;

            if (await _permissionRepository.GetById(permissionId) is null)
                return false;

            var key = PermissionValues.NormalizeKey(request.PermissionKey);
            if (await _permissionRepository.ExistsByKey(key, permissionId))
                return false;

            return await _permissionRepository.UpdatePermission(new Permission
            {
                permission_id = permissionId,
                permission_key = key,
                permission_value = PermissionValues.NormalizeRoles(request.PermissionValue)
            });
        }

        public async Task<bool> Delete(long permissionId)
        {
            return permissionId > 0 && await _permissionRepository.DeletePermission(permissionId);
        }

        public async Task<bool> HasPermission(string permissionKey, string role)
        {
            if (string.IsNullOrWhiteSpace(permissionKey) || string.IsNullOrWhiteSpace(role))
                return false;

            var permission = await _permissionRepository.GetByKey(
                PermissionValues.NormalizeKey(permissionKey));

            return permission is not null && PermissionValues.ContainsRole(permission.permission_value, role);
        }

        public async Task<IEnumerable<PermissionDto>> GetPermissionsByRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return Array.Empty<PermissionDto>();

            var permissions = await _permissionRepository.GetAll();

            return permissions
                .Where(permission => PermissionValues.ContainsRole(permission.permission_value, role))
                .Select(MapPermission)
                .ToList();
        }

        private static bool IsValidInput(string? key, string? value)
        {
            return !string.IsNullOrWhiteSpace(key) && key.Length <= 150 &&
                !string.IsNullOrWhiteSpace(value) && value.Length <= 1000 &&
                PermissionValues.NormalizeRoles(value).Length > 0;
        }

        private static PermissionDto MapPermission(Permission permission)
        {
            return new PermissionDto
            {
                PermissionId = permission.permission_id,
                PermissionKey = permission.permission_key,
                PermissionValue = permission.permission_value
            };
        }
    }
}
