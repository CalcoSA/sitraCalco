using Authentication.Application.Interfaces;
using Authentication.Domain.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WordpressUserController : ControllerBase
    {
        private readonly IWordpressUserApplication _wordpressUserApplication;
        private readonly ILogger<WordpressUserController> _logger;

        public WordpressUserController(
            IWordpressUserApplication wordpressUserApplication,
            ILogger<WordpressUserController> logger)
        {
            _wordpressUserApplication = wordpressUserApplication;
            _logger = logger;
        }

        [HttpGet("{userLogin}")]
        public async Task<IActionResult> GetByLogin(string userLogin)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userLogin))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El UserLogin es obligatorio.",
                        Result = new { }
                    });
                }

                var wordpressUser = await _wordpressUserApplication.GetByLogin(userLogin);

                if (wordpressUser is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Usuario no encontrado en WordPress.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Usuario de WordPress consultado correctamente.",
                    Result = wordpressUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar usuario de WordPress por UserLogin {UserLogin}.", userLogin);
                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al consultar el usuario de WordPress.",
                    Result = new { }
                });
            }
        }
    }
}