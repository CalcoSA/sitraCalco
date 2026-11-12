using Inventory.Domain.Helpers;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly SitraCalcoContext _context;

        public PermissionRepository(SitraCalcoContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Permission>> GetAll()
        {
            return await _context.Permissions.AsNoTracking()
                .OrderBy(permission => permission.permission_key)
                .ToListAsync();
        }

        public async Task<Permission?> GetById(long permissionId)
        {
            return await _context.Permissions.AsNoTracking()
                .FirstOrDefaultAsync(permission => permission.permission_id == permissionId);
        }

        public async Task<Permission?> GetByKey(string permissionKey)
        {
            var normalizedKey = PermissionValues.NormalizeKey(permissionKey);

            return await _context.Permissions.AsNoTracking()
                .FirstOrDefaultAsync(permission =>
                    permission.permission_key.ToUpper() == normalizedKey);
        }

        public async Task<bool> ExistsByKey(string permissionKey, long? excludePermissionId = null)
        {
            var normalizedKey = PermissionValues.NormalizeKey(permissionKey);
            var query = _context.Permissions.AsNoTracking()
                .Where(permission => permission.permission_key.ToUpper() == normalizedKey);

            if (excludePermissionId.HasValue)
                query = query.Where(permission => permission.permission_id != excludePermissionId.Value);

            return await query.AnyAsync();
        }

        public async Task<long> CreatePermission(Permission permission)
        {
            await _context.Permissions.AddAsync(permission);

            try
            {
                await _context.SaveChangesAsync();
                return permission.permission_id;
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
            {
                // La restricción UNIQUE también resuelve creaciones concurrentes de la misma key.
                _context.Entry(permission).State = EntityState.Detached;
                return 0;
            }
        }

        public async Task<bool> UpdatePermission(Permission permission)
        {
            var currentPermission = await _context.Permissions
                .FirstOrDefaultAsync(current => current.permission_id == permission.permission_id);

            if (currentPermission is null)
                return false;

            currentPermission.permission_key = permission.permission_key;
            currentPermission.permission_value = permission.permission_value;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
            {
                _context.Entry(currentPermission).State = EntityState.Detached;
                return false;
            }
        }

        public async Task<bool> DeletePermission(long permissionId)
        {
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(current => current.permission_id == permissionId);

            if (permission is null)
                return false;

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
