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
    public class ProductReceptionConfigurationController : ControllerBase
    {
        private readonly IProductReceptionConfigurationApplication _application;
        private readonly IValidator<CreateProductReceptionConfigurationDto> _createValidator;
        private readonly IValidator<UpdateProductReceptionConfigurationDto> _updateValidator;
        private readonly ILogger<ProductReceptionConfigurationController> _logger;

        public ProductReceptionConfigurationController(IProductReceptionConfigurationApplication application,
            IValidator<CreateProductReceptionConfigurationDto> createValidator,
            IValidator<UpdateProductReceptionConfigurationDto> updateValidator,
            ILogger<ProductReceptionConfigurationController> logger)
        {
            _application = application;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductReceptionConfigurationDto request)
        {
            try
            {
                var validation = await _createValidator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)),
                        Result = false
                    });
                }

                var user = User.FindFirst("userLogin")?.Value;

                if (string.IsNullOrWhiteSpace(user))
                {
                    return Unauthorized();
                }

                var response = await _application.Create(request, user);

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Configuración de recepción creada correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la configuración de recepción.");

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
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductReceptionConfigurationDto request)
        {
            try
            {
                var validation = await _updateValidator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)),
                        Result = false
                    });
                }

                var user = User.FindFirst("userLogin")?.Value;

                if (string.IsNullOrWhiteSpace(user))
                {
                    return Unauthorized();
                }

                var response = await _application.Update(id, request, user);

                if (!response)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Configuración de recepción no encontrada.",
                        Result = false
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Configuración de recepción actualizada correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la configuración de recepción.");

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
                        Message = "Configuración de recepción no encontrada.",
                        Result = false
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Configuración de recepción eliminada correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la configuración de recepción.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al eliminar la configuración de recepción.",
                        Result = false
                    });
            }
        }
    }
}