using Authentication.Application.Interfaces;
using Authentication.Domain.Dtos;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using AutoMapper;

namespace Authentication.Application.Services
{
    public class UserApplication : IUserApplication
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserApplication(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener todos los registros de la tabla User de forma paginada.
        /// </summary>
        /// <param name="page">Type: int - Página solicitada</param>
        /// <param name="take">Type: int - Cantidad de registros por página</param>
        /// <param name="search">Type: string? - Filtro opcional por UserLogin</param>
        /// <returns>Type: PagedDto UserDto - Lista paginada con usuarios y su rol asociado</returns>
        public async Task<PagedDto<UserDto>> GetAll(int page, int take, string? search)
        {
            try
            {
                var users = await _userRepository.GetAllUsers(page, take, search);

                return new PagedDto<UserDto>
                {
                    Items = _mapper.Map<IEnumerable<UserDto>>(users.Items),
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
        /// <returns>Type: UserDto - Usuario con información del rol asociado</returns>
        public async Task<UserDto> GetById(int idUser)
        {
            try
            {
                var user = await _userRepository.GetUserById(idUser);

                if (user is null)
                    return null!;

                return _mapper.Map<UserDto>(user);
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
        /// <returns>Type: bool - True si el usuario fue creado correctamente</returns>
        public async Task<bool> Create(CreateUserDto entity)
        {
            try
            {
                if (entity is null)
                    return false;

                if (string.IsNullOrWhiteSpace(entity.UserLogin))
                    return false;

                if (string.IsNullOrWhiteSpace(entity.UserName))
                    return false;

                if (entity.WordpressUserId <= 0)
                    return false;

                if (entity.IdRole <= 0)
                    return false;

                var userLogin = entity.UserLogin.Trim();
                var userName = entity.UserName.Trim();

                var roleExists = await _userRepository.GetById(entity.IdRole);

                if (roleExists == null)
                    return false;

                var userLoginExists = await _userRepository.ExistsByUserLogin(userLogin);

                if (userLoginExists)
                    return false;

                var user = new User
                {
                    WordpressUserId = entity.WordpressUserId,
                    UserLogin = userLogin,
                    UserName = userName,
                    StatusUser = entity.StatusUser
                };

                await _userRepository.CreateUser(user, entity.IdRole);

                return true;
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
        /// <returns>Type: bool - True si el usuario fue actualizado correctamente</returns>
        public async Task<bool> Update(UpdateUserDto entity)
        {
            try
            {
                if (entity is null)
                    return false;

                if (entity.IdUser <= 0)
                    return false;

                if (entity.IdRole <= 0)
                    return false;

                var currentUser = await _userRepository.GetById(entity.IdUser);

                if (currentUser is null)
                    return false;

                var roleExists = await _userRepository.GetById(entity.IdRole);

                if (roleExists == null)
                    return false;

                await _userRepository.UpdateUser(
                    entity.IdUser,
                    entity.StatusUser,
                    entity.IdRole
                );

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para eliminar un usuario por IdUser.
        /// </summary>
        /// <param name="idUser">Type: int - Identificador del usuario a eliminar</param>
        /// <returns>Type: bool - True si el usuario fue eliminado correctamente</returns>
        public async Task<bool> Delete(int idUser)
        {
            try
            {
                if (idUser <= 0)
                    return false;

                var user = await _userRepository.GetById(idUser);

                if (user is null)
                    return false;

                await _userRepository.DeleteUser(idUser);

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