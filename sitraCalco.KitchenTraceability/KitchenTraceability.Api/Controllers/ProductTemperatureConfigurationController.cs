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
    public class ProductTemperatureConfigurationController : ControllerBase
    {
        private readonly IProductTemperatureConfigurationApplication _application;
        private readonly IValidator<CreateProductTemperatureConfigurationDto> _createValidator;
        private readonly IValidator<UpdateProductTemperatureConfigurationDto> _updateValidator;
        private readonly ILogger<ProductTemperatureConfigurationController> _logger;

        public ProductTemperatureConfigurationController(IProductTemperatureConfigurationApplication application,
            IValidator<CreateProductTemperatureConfigurationDto> createValidator,
            IValidator<UpdateProductTemperatureConfigurationDto> updateValidator,
            ILogger<ProductTemperatureConfigurationController> logger)
        {
            _application = application;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductTemperatureConfigurationDto request)
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

                var userLogin = User.FindFirst("userLogin")?.Value;

                if (string.IsNullOrWhiteSpace(userLogin))
                {
                    return Unauthorized(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No fue posible identificar el usuario autenticado.",
                        Result = false
                    });
                }

                var response = await _application.Create(request, userLogin);

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Configuración de temperatura creada correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la configuración de temperatura.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Result = false
                    });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductTemperatureConfigurationDto request)
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

                var userLogin = User.FindFirst("userLogin")?.Value;

                if (string.IsNullOrWhiteSpace(userLogin))
                {
                    return Unauthorized(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No fue posible identificar el usuario autenticado.",
                        Result = false
                    });
                }

                var response = await _application.Update(id, request, userLogin);

                if (!response)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Configuración de temperatura no encontrada.",
                        Result = false
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Configuración de temperatura actualizada correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la configuración de temperatura con Id {ConfigurationId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Result = false
                    });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _application.Delete(id);

                if (!response)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Configuración de temperatura no encontrada.",
                        Result = false
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Configuración de temperatura eliminada correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la configuración de temperatura con Id {ConfigurationId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al eliminar la configuración de temperatura.",
                        Result = false
                    });
            }
        }
    }
}