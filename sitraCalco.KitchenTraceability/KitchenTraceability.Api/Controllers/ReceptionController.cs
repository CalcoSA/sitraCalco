using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Responses;
using Microsoft.AspNetCore.Authorization;
using KitchenTraceability.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace KitchenTraceability.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReceptionController : ControllerBase
    {
        private readonly IReceptionApplication _receptionApplication;
        private readonly IValidator<CreateReceptionDto> _createValidator;
        private readonly ILogger<ReceptionController> _logger;

        public ReceptionController(IReceptionApplication receptionApplication,
            IValidator<CreateReceptionDto> createValidator,
            ILogger<ReceptionController> logger)
        {
            _receptionApplication = receptionApplication;
            _createValidator = createValidator;
            _logger = logger;
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var response = await _receptionApplication.GetById(id);

                if (response is null)
                {
                    return NotFound(
                        new ResponseApi
                        {
                            IsSuccess = false,
                            Message = "Recepción no encontrada.",
                            Result = new { }
                        });
                }

                return Ok(
                    new ResponseApi
                    {
                        IsSuccess = true,
                        Message = "Recepción consultada correctamente.",
                        Result = response
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar la recepción con Id {ReceptionId}.", id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar la recepción.",
                        Result = new { }
                    });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReceptionDto request)
        {
            try
            {
                var validationResult = await _createValidator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return BadRequest(
                        new ResponseApi
                        {
                            IsSuccess = false,
                            Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                            Result = false
                        });
                }

                var userLogin = User.FindFirst("userLogin")?.Value;

                if (string.IsNullOrWhiteSpace(userLogin))
                {
                    return Unauthorized(
                        new ResponseApi
                        {
                            IsSuccess = false,
                            Message = "No fue posible identificar el usuario autenticado.",
                            Result = false
                        });
                }

                var response = await _receptionApplication.Create(request, userLogin);

                return Ok(
                    new ResponseApi
                    {
                        IsSuccess = true,
                        Message = "Recepción creada correctamente.",
                        Result = response
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Result = false
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la recepción.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al crear la recepción.",
                        Result = false
                    });
            }
        }
    }
}