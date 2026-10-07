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
    public class ReceptionTypeController : ControllerBase
    {
        private readonly IReceptionTypeApplication _receptionTypeApplication;
        private readonly IValidator<CreateReceptionTypeDto> _createValidator;
        private readonly IValidator<UpdateReceptionTypeDto> _updateValidator;
        private readonly IValidator<PaginationDto> _paginationValidator;
        private readonly ILogger<ReceptionTypeController> _logger;

        public ReceptionTypeController(IReceptionTypeApplication receptionTypeApplication,
            IValidator<CreateReceptionTypeDto> createValidator,
            IValidator<UpdateReceptionTypeDto> updateValidator,
            IValidator<PaginationDto> paginationValidator,
            ILogger<ReceptionTypeController> logger)
        {
            _receptionTypeApplication = receptionTypeApplication;
            _paginationValidator = paginationValidator;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination, [FromQuery] string? search = null)
        {
            try
            {
                var validationResult = await _paginationValidator.ValidateAsync(pagination);

                if (!validationResult.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                        Result = new { }
                    });
                }

                var response = await _receptionTypeApplication.GetAll(pagination.Page, pagination.Take, search);

                if (response.TotalRecords == 0)
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron tipos de recepción registrados.",
                        Result = response
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Tipos de recepción consultados correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los tipos de recepción.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar los tipos de recepción.",
                        Result = new { }
                    });
            }
        }

        [HttpGet("options")]
        public async Task<IActionResult> GetOptions([FromQuery] string? search = null)
        {
            try
            {
                var response = await _receptionTypeApplication.GetOptions(search);

                if (!response.Any())
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron tipos de recepción disponibles.",
                        Result = response
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Tipos de recepción consultados correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los tipos de recepción para la lista desplegable.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar los tipos de recepción.",
                        Result = new { }
                    });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var response = await _receptionTypeApplication.GetById(id);

                if (response is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Tipo de recepción no encontrado.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Tipo de recepción consultado correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el tipo de recepción con Id {ReceptionTypeId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar el tipo de recepción.",
                        Result = new { }
                    });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReceptionTypeDto request)
        {
            try
            {
                var validationResult = await _createValidator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                        Result = false
                    });
                }

                var response = await _receptionTypeApplication.Create(request);

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Tipo de recepción creado correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el tipo de recepción.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al crear el tipo de recepción.",
                        Result = false
                    });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateReceptionTypeDto request)
        {
            try
            {
                var validationResult = await _updateValidator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                        Result = false
                    });
                }

                var response = await _receptionTypeApplication.Update(id, request);

                if (!response)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Tipo de recepción no encontrado.",
                        Result = false
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Tipo de recepción actualizado correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el tipo de recepción con Id {ReceptionTypeId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al actualizar el tipo de recepción.",
                        Result = false
                    });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _receptionTypeApplication.Delete(id);

                if (!response)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Tipo de recepción no encontrado.",
                        Result = false
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Tipo de recepción eliminado correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el tipo de recepción con Id {ReceptionTypeId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al eliminar el tipo de recepción.",
                        Result = false
                    });
            }
        }
    }
}