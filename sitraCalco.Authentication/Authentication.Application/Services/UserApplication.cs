using Authentication.Application.Interfaces;
using Authentication.Domain.Dtos;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using AutoMapper;
using Authentication.Domain.Exceptions;

namespace Authentication.Application.Services
{
    public class UserApplication : IUserApplication
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;

        public UserApplication(IUserRepository userRepository, IMapper mapper, IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _roleRepository = roleRepository;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener todos los registros de la tabla User de forma paginada.
        /// </summary>
        /// <param name="page">Type: int - Página solicitada</param>
        /// <param name="take">Type: int - Cantidad de registros por página</param>
        /// <param name="search">Type: string? - Filtro opcional por UserLogin</param>
        /// <returns>Lista paginada administrativa con usuarios, rol y asignación PDV.</returns>
        public async Task<PagedDto<UserAdministrationDto>> GetAll(int page, int take, string? search)
        {
            try
            {
                var users = await _userRepository.GetAllUsers(page, take, search);

                return new PagedDto<UserAdministrationDto>
                {
                    Items = _mapper.Map<IEnumerable<UserAdministrationDto>>(users.Items),
                    Total = users.Total,
                    Page = users.Page,
                    Take = users.Take,
                    Pages = users.Pages
                };
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener un registro de la tabla User.
        /// </summary>
        /// <param name="idUser">Type: int - Identificador del usuario a buscar</param>
        /// <returns>Usuario administrativo con información del rol y la asignación PDV.</returns>
        public async Task<UserAdministrationDto> GetById(int idUser)
        {
            try
            {
                var user = await _userRepository.GetAdministrativeUserById(idUser);

                if (user is null)
                    return null!;

                return _mapper.Map<UserAdministrationDto>(user);
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para crear un usuario y su relación con un rol.
        /// </summary>
        /// <param name="entity">Type: CreateUserDto - Información del usuario a crear</param>
        /// <returns>Resultado de la operación y descripción de auditoría si fue exitosa</returns>
        public async Task<AuditResultDto> Create(CreateUserDto entity)
        {
            try
            {
                if (entity is null)
                    return new AuditResultDto();

                if (string.IsNullOrWhiteSpace(entity.UserLogin))
                    return new AuditResultDto();

                if (string.IsNullOrWhiteSpace(entity.UserName))
                    return new AuditResultDto();

                if (entity.WordpressUserId <= 0)
                    return new AuditResultDto();

                if (entity.IdRole <= 0)
                    return new AuditResultDto();

                var userLogin = entity.UserLogin.Trim();
                var userName = entity.UserName.Trim();

                var role = await _roleRepository.GetById(entity.IdRole);

                if (role == null)
                    return new AuditResultDto();

                var userLoginExists = await _userRepository.ExistsByUserLogin(userLogin);

                if (userLoginExists)
                    return new AuditResultDto();

                var user = new User
                {
                    WordpressUserId = entity.WordpressUserId,
                    UserLogin = userLogin,
                    UserName = userName,
                    StatusUser = entity.StatusUser
                };

                var assignment = await _userRepository.CreateUser(user, entity.IdRole, entity.SolutionCenterId);

                return new AuditResultDto
                {
                    IsSuccess = true,
                    Description = WithAssignmentAudit(
                        $"Se creó el usuario {user.UserName} con el rol {role.NameRole}.", assignment)
                };
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para actualizar un registro en la tabla User.
        /// </summary>
        /// <param name="entity">Type: UpdateUserDto - Información del usuario a actualizar</param>
        /// <returns>Resultado de la operación y descripción de auditoría si fue exitosa</returns>
        public async Task<AuditResultDto> Update(UpdateUserDto entity)
        {
            try
            {
                if (entity is null)
                    return new AuditResultDto();

                if (entity.IdUser <= 0)
                    return new AuditResultDto();

                if (entity.IdRole <= 0)
                    return new AuditResultDto();

                var currentUser = await _userRepository.GetById(entity.IdUser);

                if (currentUser is null)
                    return new AuditResultDto();

                var role = await _roleRepository.GetById(entity.IdRole);

                if (role == null)
                    return new AuditResultDto();

                var assignment = await _userRepository.UpdateUser(
                    entity.IdUser,
                    entity.StatusUser,
                    entity.IdRole,
                    entity.SolutionCenterId
                );

                return new AuditResultDto
                {
                    IsSuccess = true,
                    Description = WithAssignmentAudit(
                        $"Se actualizó el usuario {currentUser.UserName}: rol {role.NameRole} y estado {(entity.StatusUser ? "activo" : "inactivo")}.", assignment)
                };
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        private static string WithAssignmentAudit(string description, UserAssignmentResultDto assignment)
        {
            var previous = assignment.PreviousSolutionCenterCode;
            var current = assignment.SolutionCenterCode;
            if (previous == current)
                return description;

            var detail = previous is null ? $" Se asignó el PDV {current}."
                : current is null ? $" Se eliminó la asignación del PDV {previous}."
                : $" Se cambió el PDV de {previous} a {current}.";
            // The audit description column is limited to 500 characters.
            var available = 500 - detail.Length;
            if (description.Length > available)
                description = description[..(available - 1)] + "…";
            return description + detail;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para eliminar un usuario por IdUser.
        /// </summary>
        /// <param name="idUser">Type: int - Identificador del usuario a eliminar</param>
        /// <returns>Resultado de la operación y descripción de auditoría si fue exitosa</returns>
        public async Task<AuditResultDto> Delete(int idUser)
        {
            try
            {
                if (idUser <= 0)
                    return new AuditResultDto();

                var user = await _userRepository.GetById(idUser);

                if (user is null)
                    return new AuditResultDto();

                var userName = user.UserName;
                await _userRepository.DeleteUser(idUser);

                return new AuditResultDto
                {
                    IsSuccess = true,
                    Description = $"Se eliminó el usuario {userName}."
                };
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }
    }
}
