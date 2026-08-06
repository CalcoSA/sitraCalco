using Authentication.Application.Interfaces;
using Authentication.Domain.Dtos;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using AutoMapper;

namespace Authentication.Application.Services
{
    public class RoleApplication : IRoleApplication
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;

        public RoleApplication(IRoleRepository roleRepository, IMapper mapper)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener todos los registros de la tabla Role
        /// </summary>
        /// <returns>Type: IEnumerable - Lista con la información solicitada de la tabla Role</returns>
        public async Task<IEnumerable<RoleDto>> GetAll()
        {
            try
            {
                var role = await _roleRepository.GetAll();
                var responseRole = _mapper.Map<IEnumerable<RoleDto>>(role);
                return responseRole;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener un Role por IdRole.
        /// </summary>
        /// <param name="idRole">Type: int - Identificador del IdRole a buscar</param>
        /// <returns>Type: RoleDetailDto - Rol con sus opciones de menú asociadas</returns>
        public async Task<RoleDetailDto> GetById(int idRole)
        {
            try
            {
                var role = await _roleRepository.GetByIdRole(idRole);

                if (role is null)
                    return null!;

                var responseRole = _mapper.Map<RoleDetailDto>(role);
                return responseRole;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para crear un rol y sus opciones de menú asociadas.
        /// </summary>
        /// <param name="entity">Type: CreateRoleDto - Información del rol a crear</param>
        /// <returns>Type: bool - True si el rol fue creado correctamente</returns>
        public async Task<bool> Create(CreateRoleDto entity)
        {
            try
            {
                if (entity is null || string.IsNullOrWhiteSpace(entity.NameRole))
                    return false;

                var roleNameExists = await _roleRepository.ExistsByName(entity.NameRole);

                if (roleNameExists)
                    return false;

                var menuOptionsExist = await _roleRepository.MenuOptionsExist(entity.MenuOptionIds);

                if (!menuOptionsExist)
                    return false;

                var role = new Role
                {
                    NameRole = entity.NameRole.Trim(),
                    StatusRole = entity.StatusRole
                };

                await _roleRepository.CreateRole(role, entity.MenuOptionIds);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para actualizar un rol y sus opciones de menú asociadas.
        /// </summary>
        /// <param name="entity">Type: UpdateRoleDto - Información del rol a actualizar</param>
        /// <returns>Type: bool - True si el rol fue actualizado correctamente</returns>
        public async Task<bool> Update(UpdateRoleDto entity)
        {
            try
            {
                if (entity is null || entity.IdRole <= 0 || string.IsNullOrWhiteSpace(entity.NameRole))
                    return false;

                var currentRole = await _roleRepository.GetById(entity.IdRole);

                if (currentRole is null)
                    return false;

                var roleNameExists = await _roleRepository.ExistsByName(entity.NameRole, entity.IdRole);

                if (roleNameExists)
                    return false;

                var menuOptionsExist = await _roleRepository.MenuOptionsExist(entity.MenuOptionIds);

                if (!menuOptionsExist)
                    return false;

                var role = new Role
                {
                    IdRole = entity.IdRole,
                    NameRole = entity.NameRole.Trim(),
                    StatusRole = entity.StatusRole
                };

                await _roleRepository.UpdateRole(role, entity.MenuOptionIds);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para eliminar un rol por IdRole.
        /// </summary>
        /// <param name="idRole">Type: int - Identificador del rol a eliminar</param>
        /// <returns>Type: bool - True si el rol fue eliminado correctamente</returns>
        public async Task<bool> Delete(int idRole)
        {
            try
            {
                if (idRole <= 0)
                    return false;

                var role = await _roleRepository.GetById(idRole);

                if (role is null)
                    return false;

                await _roleRepository.DeleteRole(idRole);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }
    }
}