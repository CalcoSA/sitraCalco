using Authentication.Infrastructure.Persistance.Data;
using Authentication.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Authentication.Domain.Models;
using Authentication.Domain.Dtos;
using Authentication.Domain.Exceptions;

namespace Authentication.Infrastructure.Persistance.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly SitraCalcoContext _context;
        public UserRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Consulta que obtine todos los usuarios de forma paginada, permitiendo filtrar por UserLogin.
        /// </summary>
        /// <param name="page">Type: int - Página solicitada</param>
        /// <param name="take">Type: int - Cantidad de registros por página</param>
        /// <param name="search">Type: string? - Filtro opcional por UserLogin</param>
        /// <returns>Type: PagedDto User - Lista paginada de usuarios</returns>
        public async Task<PagedDto<User>> GetAllUsers(int page, int take, string? search)
        {
            if (page <= 0)
                page = 1;

            if (take <= 0)
                take = 10;

            var query = _context.Users
                .AsNoTracking()
                .Include(x => x.Userroles)
                    .ThenInclude(x => x.IdRoleNavigation)
                .Include(x => x.Userroles)
                    .ThenInclude(x => x.SolutionCenter)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();

                query = query.Where(x =>
                    x.UserLogin.ToLower().Contains(normalizedSearch));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.UserLogin)
                .Skip((page - 1) * take)
                .Take(take)
                .ToListAsync();

            return new PagedDto<User>
            {
                Items = items,
                Total = total,
                Page = page,
                Take = take,
                Pages = total > 0
                    ? Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(total) / take))
                    : 0
            };
        }

        /// <summary>
        /// Consulta un usuario por IdUser, incluyendo su rol asociado.
        /// </summary>
        /// <param name="idUser">Type: int - Identificador del usuario</param>
        /// <returns>Type: User - Usuario encontrado con su rol</returns>
        public async Task<User?> GetUserById(int idUser)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(x => x.Userroles)
                    .ThenInclude(x => x.IdRoleNavigation)
                .FirstOrDefaultAsync(x => x.IdUser == idUser);
        }

        public async Task<User?> GetAdministrativeUserById(int idUser)
        {
            return await _context.Users.AsNoTracking()
                .Include(x => x.Userroles).ThenInclude(x => x.IdRoleNavigation)
                .Include(x => x.Userroles).ThenInclude(x => x.SolutionCenter)
                .FirstOrDefaultAsync(x => x.IdUser == idUser);
        }

        /// <summary>
        /// Consulta un usuario por UserLogin, incluyendo su rol asociado.
        /// </summary>
        /// <param name="userLogin">Type: string - Login del usuario</param>
        /// <returns>Type: User - Usuario encontrado con su rol asociado</returns>
        public async Task<User?> GetUserByLogin(string userLogin)
        {
            var normalizedUserLogin = userLogin.Trim().ToLower();

            return await _context.Users
                .AsNoTracking()
                .Include(x => x.Userroles)
                    .ThenInclude(x => x.IdRoleNavigation)
                .FirstOrDefaultAsync(x => x.UserLogin.ToLower() == normalizedUserLogin);
        }

        /// <summary>
        /// Valida si existe un usuario con el mismo UserLogin, ignorando mayúsculas y minúsculas.
        /// </summary>
        /// <param name="userLogin">Type: string - Login del usuario</param>
        /// <param name="excludeIdUser">Type: int? - IdUser a excluir en actualización</param>
        /// <returns>Type: bool - True si ya existe un usuario con ese login</returns>
        public async Task<bool> ExistsByUserLogin(string userLogin, int? excludeIdUser = null)
        {
            var normalizedUserLogin = userLogin.Trim().ToLower();

            var query = _context.Users
                .AsNoTracking()
                .Where(x => x.UserLogin.ToLower() == normalizedUserLogin);

            if (excludeIdUser.HasValue)
            {
                query = query.Where(x => x.IdUser != excludeIdUser.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Crea un usuario y registra su relación con un rol.
        /// </summary>
        /// <param name="user">Type: User - Entidad del usuario a crear</param>
        /// <param name="idRole">Type: int - Rol asociado al usuario</param>
        /// <returns></returns>
        public async Task<UserAssignmentResultDto> CreateUser(User user, int idRole, int? solutionCenterId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var center = await ValidateAssignment(idRole, solutionCenterId);
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                var userRole = new Userrole
                {
                    IdUser = user.IdUser,
                    IdRole = idRole,
                    SolutionCenterId = center?.SolutionCenterId
                };

                await _context.Userroles.AddAsync(userRole);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return new UserAssignmentResultDto { SolutionCenterCode = center?.SolutionCenterCode };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Actualiza únicamente el estado del usuario y reemplaza su rol asociado.
        /// </summary>
        /// <param name="idUser">Type: int - Identificador del usuario</param>
        /// <param name="statusUser">Type: bool - Estado del usuario</param>
        /// <param name="idRole">Type: int - Nuevo rol asociado</param>
        /// <returns></returns>
        public async Task<UserAssignmentResultDto> UpdateUser(int idUser, bool statusUser, int idRole, int? solutionCenterId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Lock the owner even when its status is unchanged: concurrent PUTs
                // must not create multiple role rows while replacing the assignment.
                var users = await _context.Users
                    .FromSqlInterpolated($"SELECT * FROM sitracalco_authentication_users WHERE IdUser = {idUser} FOR UPDATE")
                    .AsNoTracking().ToListAsync();
                var currentUser = users.SingleOrDefault();

                if (currentUser is null)
                    throw new BusinessRuleException("El usuario no existe.");

                var center = await ValidateAssignment(idRole, solutionCenterId);
                var trackedUser = _context.Users.Local.FirstOrDefault(x => x.IdUser == idUser);
                if (trackedUser is not null)
                    _context.Entry(trackedUser).State = EntityState.Detached;
                _context.Attach(currentUser);

                currentUser.StatusUser = statusUser;

                var currentUserRoles = await _context.Userroles
                    .Include(x => x.SolutionCenter)
                    .Where(x => x.IdUser == idUser)
                    .ToListAsync();
                var previousCode = currentUserRoles
                    .Select(x => x.SolutionCenter?.SolutionCenterCode)
                    .FirstOrDefault(x => x is not null);

                if (currentUserRoles.Any())
                {
                    _context.Userroles.RemoveRange(currentUserRoles);
                }

                // Delete before insert to preserve the existing unique (IdUser, IdRole)
                // constraint when the selected role has not changed.
                await _context.SaveChangesAsync();

                var newUserRole = new Userrole
                {
                    IdUser = idUser,
                    IdRole = idRole,
                    SolutionCenterId = center?.SolutionCenterId
                };

                await _context.Userroles.AddAsync(newUserRole);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new UserAssignmentResultDto
                {
                    PreviousSolutionCenterCode = previousCode,
                    SolutionCenterCode = center?.SolutionCenterCode
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<SolutionCenter?> ValidateAssignment(int idRole, int? solutionCenterId)
        {
            // Called only inside the write transaction. Shared locks keep the role
            // and catalog entry valid until commit without serializing readers.
            var roles = await _context.Roles
                .FromSqlInterpolated($"SELECT * FROM sitracalco_authentication_role WHERE IdRole = {idRole} LOCK IN SHARE MODE")
                .AsNoTracking().ToListAsync();
            var role = roles.SingleOrDefault()
                ?? throw new BusinessRuleException("El rol seleccionado no existe.");

            if (role.RoleCode != Role.PointOfSaleCode)
                return null;

            if (!solutionCenterId.HasValue)
                throw new BusinessRuleException("Debe seleccionar un punto de venta para el rol PDV.");
            if (solutionCenterId.Value <= 0)
                throw new BusinessRuleException("El identificador del punto de venta debe ser mayor a cero.");

            var centers = await _context.SolutionCenters
                .FromSqlInterpolated($"SELECT * FROM sitracalco_authentication_solution_center WHERE solution_center_id = {solutionCenterId.Value} LOCK IN SHARE MODE")
                .AsNoTracking().ToListAsync();
            return centers.SingleOrDefault()
                ?? throw new BusinessRuleException("El punto de venta seleccionado no existe.");
        }

        /// <summary>
        /// Elimina un usuario y sus relaciones con roles.
        /// </summary>
        /// <param name="idUser">Type: int - Identificador del usuario</param>
        /// <returns></returns>
        public async Task DeleteUser(int idUser)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.IdUser == idUser);

                if (user is null)
                    throw new InvalidOperationException("El usuario no existe.");

                var userRoles = await _context.Userroles
                    .Where(x => x.IdUser == idUser)
                    .ToListAsync();

                if (userRoles.Any())
                {
                    _context.Userroles.RemoveRange(userRoles);
                }

                _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
