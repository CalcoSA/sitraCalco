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
    public class ProductController : ControllerBase
    {
        private readonly IProductApplication _productApplication;
        private readonly IValidator<PaginationDto> _paginationValidator;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductApplication productApplication,
            IValidator<PaginationDto> paginationValidator,
            ILogger<ProductController> logger)
        {
            _productApplication = productApplication;
            _paginationValidator = paginationValidator;
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

                var response = await _productApplication.GetAll(pagination.Page, pagination.Take, search);

                if (response.TotalRecords == 0)
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron productos registrados.",
                        Result = response
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Productos consultados correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los productos.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar los productos.",
                        Result = new { }
                    });
            }
        }

        [HttpGet("options")]
        public async Task<IActionResult> GetOptions([FromQuery] string? search = null)
        {
            try
            {
                var response = await _productApplication.GetOptions(search);

                if (!response.Any())
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron productos.",
                        Result = response
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Productos consultados correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los productos para la lista desplegable.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar los productos.",
                        Result = new { }
                    });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var response = await _productApplication.GetById(id);

                if (response is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Producto no encontrado.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Producto consultado correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el producto con Id {ProductId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar el producto.",
                        Result = new { }
                    });
            }
        }

        [HttpPost("siesa")]
        public async Task<IActionResult> GetSiesaProducts()
        {
            try
            {
                var response = await _productApplication.GetSiesaProducts();

                if (!response.NewProductsSaved)
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = true,
                        Message = "No se encontraron productos nuevos para registrar desde SIESA.",
                        Result = response
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = $"Se registraron {response.NewProductsCount} productos nuevos desde SIESA.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sincronizar los productos de SIESA.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al sincronizar los productos de SIESA.",
                        Result = new { }
                    });
            }
        }
    }
}