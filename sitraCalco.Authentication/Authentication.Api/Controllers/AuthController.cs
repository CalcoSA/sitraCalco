using Authentication.Application.Interfaces;
using Authentication.Domain.Dtos;
using Authentication.Domain.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthApplication _authApplication;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthApplication authApplication,
            ILogger<AuthController> logger)
        {
            _authApplication = authApplication;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            try
            {
                var authResponse = await _authApplication.Login(request);

                if (authResponse is null)
                {
                    return Unauthorized(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Usuario o contraseña incorrectos.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Login exitoso.",
                    Result = authResponse
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ResponseApi
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Result = new { }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login para usuario {Username}.", request?.Username);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al iniciar sesión.",
                    Result = new { }
                });
            }
        }

        [HttpGet("intranet-access")]
        public async Task<IActionResult> IntranetAccess([FromQuery] string userLogin, [FromQuery] long ts, [FromQuery] string sig)
        {
            try
            {
                var request = new IntranetAccessDto
                {
                    UserLogin = userLogin,
                    Ts = ts,
                    Sig = sig
                };

                var authResponse = await _authApplication.IntranetAccess(request);

                if (authResponse is null)
                {
                    return Unauthorized(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Acceso no autorizado. La firma no es válida, expiró o el usuario no tiene permisos.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Acceso desde intranet exitoso.",
                    Result = authResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en acceso de intranet para usuario {UserLogin}.", userLogin);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al validar el acceso desde intranet.",
                    Result = new { }
                });
            }
        }
    }
}