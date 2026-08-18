using Authentication.Application.Interfaces;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Dtos;
using AutoMapper;

namespace Authentication.Application.Services
{
    public class AuthApplication : IAuthApplication
    {
        private readonly IWordpressUserRepository _wordpressUserRepository;
        private readonly ISSOSignatureApplication _ssoSignatureApplication;
        private readonly IMenuOptionApplication _menuOptionApplication;
        private readonly IJwtTokenApplication _jwtTokenApplication;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public AuthApplication(IWordpressUserRepository wordpressUserRepository,
            ISSOSignatureApplication ssoSignatureApplication,
            IMenuOptionApplication menuOptionApplication,
            IJwtTokenApplication jwtTokenApplication,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _wordpressUserRepository = wordpressUserRepository;
            _ssoSignatureApplication = ssoSignatureApplication;
            _menuOptionApplication = menuOptionApplication;
            _jwtTokenApplication = jwtTokenApplication;
            _userRepository = userRepository;  
            _mapper = mapper;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para autenticar un usuario con credenciales de WordPress y valida autorización local.
        /// </summary>
        /// <param name="loginData">Type: LoginDto - Usuario y contraseña</param>
        /// <returns>Type: AuthUserDto - Información del usuario autentificado</returns>
        public async Task<AuthUserDto?> Login(LoginDto loginData)
        {
            try
            {
                if (loginData is null)
                    return null;

                if (string.IsNullOrWhiteSpace(loginData.Username) || string.IsNullOrWhiteSpace(loginData.Password))
                    return null;

                var username = loginData.Username.Trim();

                var wordpressUser = await _wordpressUserRepository.GetByLogin(username);

                if (wordpressUser is null)
                    return null;

                if (string.IsNullOrWhiteSpace(wordpressUser.WordpressUserPass))
                    return null;

                if (string.IsNullOrWhiteSpace(wordpressUser.WordpressUserLogin))
                    return null;

                var passwordIsValid = _wordpressUserRepository.Verify(loginData.Password, wordpressUser.WordpressUserPass);

                if (!passwordIsValid)
                    return null;

                return await Authorize(wordpressUser.WordpressUserLogin);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para autenticar el acceso desde intranet usando userLogin, ts y sig.
        /// </summary>
        /// <param name="accessData">Type: IntranetAccessDto - Parámetros SSO de intranet</param>
        /// <returns>Type: AuthUserDto - Información del usuario autentificado</returns>
        public async Task<AuthUserDto?> IntranetAccess(IntranetAccessDto accessData)
        {
            try
            {
                if (accessData is null)
                    return null;

                if (string.IsNullOrWhiteSpace(accessData.UserLogin) || string.IsNullOrWhiteSpace(accessData.Sig))
                    return null;

                var userLogin = accessData.UserLogin.Trim();
                var sig = accessData.Sig.Trim();

                var signatureIsValid = _ssoSignatureApplication.IsValid(userLogin, accessData.Ts, sig);

                if (!signatureIsValid)
                    return null;

                var wordpressUser = await _wordpressUserRepository.GetByLogin(userLogin);

                if (wordpressUser is null)
                    return null;

                if (string.IsNullOrWhiteSpace(wordpressUser.WordpressUserLogin))
                    return null;

                return await Authorize(wordpressUser.WordpressUserLogin);
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método privado que valida si el usuario autenticado en WordPress existe y está autorizado en el sistema local.
        /// </summary>
        /// <param name="userLogin">Type: string - UserLogin del usuario autenticado en WordPress</param>
        /// <returns>Type: AuthUserDto - Información del usuario autentificado</returns>
        private async Task<AuthUserDto?> Authorize(string userLogin)
        {
            var localUser = await _userRepository.GetUserByLogin(userLogin);

            if (localUser is null)
                throw new UnauthorizedAccessException("El usuario no tiene permisos asignados en el aplicativo.");

            if (!localUser.StatusUser)
                throw new UnauthorizedAccessException("El usuario está inactivo en el aplicativo.");

            var user = _mapper.Map<UserDto>(localUser);

            var menuOptions = (await _menuOptionApplication.GetByRole(user.IdRole)).ToList();

            if (!menuOptions.Any())
                throw new UnauthorizedAccessException("El usuario no tiene permisos asignados en el aplicativo.");

            var tokenData = _jwtTokenApplication.GenerateToken(user);

            return new AuthUserDto
            {
                TokenType = "Bearer",
                AccessToken = tokenData.Token,
                ExpiresAt = tokenData.ExpiresAt,
                User = user,
                MenuOptions = menuOptions
            };
        }
    }
}