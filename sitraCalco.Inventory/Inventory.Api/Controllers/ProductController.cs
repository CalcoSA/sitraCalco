using Inventory.Application.Interfaces;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductApplication _productApplication;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductApplication productApplication,
            ILogger<ProductController> logger)
        {
            _productApplication = productApplication;
            _logger = logger;
        }

        /// <summary>
        /// Consulta los productos autorizados en SIESA
        /// y crea o actualiza los productos en SITRA.
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncProducts()
        {
            try
            {
                var processedProducts =
                    await _productApplication.SyncProducts();

                if (processedProducts <= 0)
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron productos válidos para sincronizar.",
                        Result = new
                        {
                            Processed = processedProducts
                        }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Productos sincronizados correctamente.",
                    Result = new
                    {
                        Processed = processedProducts
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al sincronizar los productos desde SIESA.");

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al sincronizar los productos desde SIESA.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Consulta paginadamente los productos almacenados en SITRA.
        /// </summary>
        /// <param name="page">Número de página.</param>
        /// <param name="take">Cantidad de registros por página.</param>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int take = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                if (page <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El número de página debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (take <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La cantidad de registros debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                var products = await _productApplication.GetPaged(
                    page,
                    take,
                    search);

                if (!products.Items.Any())
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron productos.",
                        Result = products
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Productos consultados correctamente.",
                    Result = products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar los productos. Página {Page}, Take {Take}, Search {Search}.",
                    page,
                    take,
                    search);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al consultar los productos.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Busca productos por referencia o nombre.
        /// Diseñado para autocompletado y selección de productos.
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts(
            [FromQuery] string search,
            [FromQuery] int take = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Debe ingresar una referencia o nombre de producto.",
                        Result = new { }
                    });
                }

                if (search.Trim().Length < 2)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La búsqueda debe contener al menos 2 caracteres.",
                        Result = new { }
                    });
                }

                if (take <= 0)
                    take = 20;

                if (take > 50)
                    take = 50;

                var products = (
                    await _productApplication.SearchProducts(
                        search,
                        take)
                ).ToList();

                if (!products.Any())
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron productos.",
                        Result = products
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Productos consultados correctamente.",
                    Result = products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al buscar productos. Search: {Search}.",
                    search);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al buscar los productos.",
                    Result = new { }
                });
            }
        }
    }
}