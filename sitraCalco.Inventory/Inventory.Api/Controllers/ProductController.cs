using Inventory.Api.Extensions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Inventory.Domain.Helpers;
using Inventory.Domain.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductApplication _productApplication;
        private readonly ILogger<ProductController> _logger;

        private readonly ILogApplication _logApplication;
        private readonly GoogleCloudStorageOptions _storageOptions;

        public ProductController(
            IProductApplication productApplication,
            ILogApplication logApplication,
            ILogger<ProductController> logger,
            IOptions<GoogleCloudStorageOptions> storageOptions)
        {
            _productApplication = productApplication;
            _logApplication = logApplication;
            _logger = logger;
            _storageOptions = storageOptions.Value;
        }

        [HttpPost("{productId:long}/image")]
        [Consumes("multipart/form-data")]
        public Task<IActionResult> UploadImage(long productId, IFormFile? file)
        {
            return SaveImage(productId, file, replace: false);
        }

        [HttpPut("{productId:long}/image")]
        [Consumes("multipart/form-data")]
        public Task<IActionResult> ReplaceImage(long productId, IFormFile? file)
        {
            return SaveImage(productId, file, replace: true);
        }

        [HttpGet("{productId:long}/image")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetImage(long productId)
        {
            try
            {
                if (productId <= 0)
                    return ImageError(400, "El identificador del producto debe ser mayor a cero.");

                var result = await _productApplication.GetImage(productId);
                return ImageResponse(result, "Imagen del producto consultada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar la imagen del producto {ProductId}.", productId);
                return ImageError(500, "Ocurrió un error al consultar la imagen del producto.");
            }
        }

        [HttpDelete("{productId:long}/image")]
        public async Task<IActionResult> DeleteImage(long productId)
        {
            var userLogin = User.GetUserLogin();
            try
            {
                if (productId <= 0)
                    return ImageError(400, "El identificador del producto debe ser mayor a cero.");

                if (string.IsNullOrWhiteSpace(userLogin))
                    return ImageError(403, "El token no contiene un userLogin válido.");

                var result = await _productApplication.DeleteImage(productId, userLogin);
                if (result.Status != ProductImageStatus.Success)
                    return ImageResponse(result, "Imagen del producto eliminada correctamente.");

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Imagen del producto eliminada correctamente.",
                    Result = new { ProductId = productId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la imagen del producto {ProductId}.", productId);
                return ImageError(500, "Ocurrió un error al eliminar la imagen del producto.");
            }
        }

        private async Task<IActionResult> SaveImage(long productId, IFormFile? file, bool replace)
        {
            var userLogin = User.GetUserLogin();
            try
            {
                if (productId <= 0)
                    return ImageError(400, "El identificador del producto debe ser mayor a cero.");

                if (string.IsNullOrWhiteSpace(userLogin))
                    return ImageError(403, "El token no contiene un userLogin válido.");

                if (file is null)
                    return ImageError(400, "Debe enviar una imagen en el campo file.");

                var validationError = ProductImageFiles.Validate(
                    file.FileName, file.ContentType, file.Length, _storageOptions.MaxImageSizeBytes);
                if (validationError is not null)
                    return ImageError(400, validationError);

                await using var stream = file.OpenReadStream();
                var result = replace
                    ? await _productApplication.ReplaceImage(productId, stream, file.FileName, file.ContentType, file.Length, userLogin)
                    : await _productApplication.UploadImage(productId, stream, file.FileName, file.ContentType, file.Length, userLogin);

                return ImageResponse(result, replace
                    ? "Imagen del producto actualizada correctamente."
                    : "Imagen del producto cargada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la imagen del producto {ProductId}. Reemplazo: {Replace}.", productId, replace);
                return ImageError(500, "Ocurrió un error al guardar la imagen del producto.");
            }
        }

        private IActionResult ImageResponse(ProductImageResultDto result, string successMessage)
        {
            return result.Status switch
            {
                ProductImageStatus.Success => Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = successMessage,
                    Result = result.Data!
                }),
                ProductImageStatus.ProductNotFound => ImageError(404, "El producto no existe."),
                ProductImageStatus.ImageNotFound => ImageError(404, "El producto no tiene una imagen."),
                ProductImageStatus.ImageAlreadyExists => ImageError(400, "El producto ya tiene una imagen. Utilice PUT para reemplazarla."),
                ProductImageStatus.Conflict => ImageError(409, "La imagen del producto cambió durante la carga. Consulte el producto e intente nuevamente."),
                _ => throw new InvalidOperationException("Resultado de imagen no reconocido.")
            };
        }

        private ObjectResult ImageError(int statusCode, string message)
        {
            return StatusCode(statusCode, new ResponseApi
            {
                IsSuccess = false,
                Message = message,
                Result = new { }
            });
        }

        /// <summary>
        /// Consulta los productos autorizados en SIESA
        /// y crea o actualiza los productos en SITRA.
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncProducts()
        {
            var userName = User.GetUserLogin();

            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El token no contiene un userLogin válido.",
                        Result = new { }
                    });
                }

                var syncResult =
                    await _productApplication.SyncProducts();

                if (syncResult.Processed <= 0)
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se encontraron productos válidos para sincronizar.",
                        Result = syncResult
                    });
                }

                await _logApplication.CreateLog(
    new CreateLogDto
    {
        Action = "Sincronizar",
        Module = "Productos",
        Description =
            $"Sincronización de productos finalizada. " +
            $"Procesados: {syncResult.Processed}, " +
            $"nuevos: {syncResult.Created}, " +
            $"actualizados: {syncResult.Updated}, " +
            $"sin cambios: {syncResult.Unchanged}.",
        UserName = userName.Trim()
    });

                foreach (var product in syncResult.CreatedProducts)
                {
                    await _logApplication.CreateLog(
                        new CreateLogDto
                        {
                            Action = "Crear",
                            Module = "Productos",
                            Description =
                                $"Se creó el producto {product.ProductName}, " +
                                $"referencia {product.Reference}, " +
                                $"unidad de medida {product.UnitOfMeasure}" +
                                $"{(string.IsNullOrWhiteSpace(product.PlanId)
                                    ? "."
                                    : $", plan {product.PlanId}.")}",
                            UserName = userName.Trim()
                        });
                }

                foreach (var product in syncResult.UpdatedProducts)
                {
                    await _logApplication.CreateLog(
                        new CreateLogDto
                        {
                            Action = "Actualizar",
                            Module = "Productos",
                            Description =
                                $"Se actualizó el producto {product.ProductName}, " +
                                $"referencia {product.Reference}, " +
                                $"unidad de medida {product.UnitOfMeasure}" +
                                $"{(string.IsNullOrWhiteSpace(product.PlanId)
                                    ? "."
                                    : $", plan {product.PlanId}.")}",
                            UserName = userName.Trim()
                        });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Productos sincronizados correctamente.",
                    Result = syncResult
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
                    Message =
                        "Ocurrió un error al sincronizar los productos desde SIESA.",
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
