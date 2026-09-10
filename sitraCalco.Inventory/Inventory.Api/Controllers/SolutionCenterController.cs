using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SolutionCenterController : ControllerBase
    {
        private readonly ISolutionCenterApplication _solutionCenterApplication;
        private readonly ILogger<SolutionCenterController> _logger;

        public SolutionCenterController(
            ISolutionCenterApplication solutionCenterApplication,
            ILogger<SolutionCenterController> logger)
        {
            _solutionCenterApplication = solutionCenterApplication;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene los tipos de bodegas y puntos de venta.
        /// </summary>
        [HttpGet("types")]
        public async Task<IActionResult> GetSolutionCenterTypes()
        {
            try
            {
                var types = (
                    await _solutionCenterApplication
                        .GetSolutionCenterTypes()
                ).ToList();

                if (!types.Any())
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No hay tipos de bodegas y puntos de venta registrados.",
                        Result = types
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Tipos de bodegas y puntos de venta consultados correctamente.",
                    Result = types
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar los tipos de bodegas y puntos de venta.");

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar los tipos de bodegas y puntos de venta.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Crea una nueva bodega.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSolutionCenter(
            [FromBody] CreateSolutionCenterDto request)
        {
            try
            {
                if (request is null ||
                    request.SolutionCenterTypeId <= 0 ||
                    string.IsNullOrWhiteSpace(request.SolutionCenterCode) ||
                    string.IsNullOrWhiteSpace(request.SolutionCenterName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información de la bodega no es válida.",
                        Result = new { }
                    });
                }

                var solutionCenterId =
                    await _solutionCenterApplication
                        .CreateSolutionCenter(request);

                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo crear la bodega. " +
                            "Verifique que el tipo sea válido y que el código o nombre no estén registrados.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Bodega creada correctamente.",
                    Result = new
                    {
                        SolutionCenterId = solutionCenterId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear la bodega.");

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al crear la bodega.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Guarda la configuración de una sección de la bodega.
        /// </summary>
        [HttpPost("{solutionCenterId:long}/sections")]
        public async Task<IActionResult> CreateSectionConfiguration(
            long solutionCenterId,
            [FromBody] CreateSectionConfigurationDto request)
        {
            try
            {
                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la bodega debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información de la sección no es válida.",
                        Result = new { }
                    });
                }

                var sectionId =
                    await _solutionCenterApplication
                        .CreateSectionConfiguration(
                            solutionCenterId,
                            request);

                if (sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo guardar la configuración de la sección. " +
                            "Verifique que la bodega exista, que el nombre de la sección no esté repetido, " +
                            "que los productos sean válidos y que no existan combinaciones repetidas " +
                            "de nombre y unidad de medida.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Configuración de la sección guardada correctamente.",
                    Result = new
                    {
                        SectionId = sectionId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al guardar la configuración de la sección para la bodega {SolutionCenterId}.",
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al guardar la configuración de la sección.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Obtiene el listado paginado de bodegas y puntos de venta según el rol.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSolutionCenters(
            [FromQuery] string role,
            [FromQuery] int page = 1,
            [FromQuery] int take = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(role))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El rol es obligatorio.",
                        Result = new { }
                    });
                }

                if (page <= 0 || take <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "Page y Take deben ser mayores a cero.",
                        Result = new { }
                    });
                }

                var solutionCenters =
                    await _solutionCenterApplication
                        .GetPagedSolutionCenters(
                            role,
                            page,
                            take);

                if (solutionCenters is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El rol enviado no es válido.",
                        Result = new { }
                    });
                }

                if (!solutionCenters.Items.Any())
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No hay bodegas o puntos de venta disponibles para el rol.",
                        Result = solutionCenters
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Bodegas y puntos de venta consultados correctamente.",
                    Result = solutionCenters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar las bodegas y puntos de venta para el rol {Role}.",
                    role);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar las bodegas y puntos de venta.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Obtiene el detalle de una bodega.
        /// </summary>
        [HttpGet("{solutionCenterId:long}")]
        public async Task<IActionResult> GetSolutionCenterById(
            long solutionCenterId)
        {
            try
            {
                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la bodega debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                var solutionCenter =
                    await _solutionCenterApplication
                        .GetSolutionCenterById(
                            solutionCenterId);

                if (solutionCenter is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La bodega no existe.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Bodega consultada correctamente.",
                    Result = solutionCenter
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar la bodega {SolutionCenterId}.",
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar la bodega.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Actualiza el estado activo o inactivo de una bodega.
        /// </summary>
        [HttpPatch("{solutionCenterId:long}/status")]
        public async Task<IActionResult> UpdateSolutionCenterStatus(
            long solutionCenterId,
            [FromBody] UpdateStatusDto request)
        {
            try
            {
                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la bodega debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información del estado no es válida.",
                        Result = new { }
                    });
                }

                var updated =
                    await _solutionCenterApplication
                        .UpdateSolutionCenterStatus(
                            solutionCenterId,
                            request.IsActive);

                if (!updated)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La bodega no existe o no se pudo actualizar.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        request.IsActive
                            ? "Bodega activada correctamente."
                            : "Bodega inactivada correctamente.",
                    Result = new
                    {
                        SolutionCenterId = solutionCenterId,
                        IsActive = request.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar el estado de la bodega {SolutionCenterId}.",
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al actualizar el estado de la bodega.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Actualiza el estado activo o inactivo de una sección.
        /// </summary>
        [HttpPatch("sections/{sectionId:long}/status")]
        public async Task<IActionResult> UpdateSectionStatus(
            long sectionId,
            [FromBody] UpdateStatusDto request)
        {
            try
            {
                if (sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la sección debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información del estado no es válida.",
                        Result = new { }
                    });
                }

                var updated =
                    await _solutionCenterApplication
                        .UpdateSectionStatus(
                            sectionId,
                            request.IsActive);

                if (!updated)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La sección no existe o no se pudo actualizar.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        request.IsActive
                            ? "Sección activada correctamente."
                            : "Sección inactivada correctamente.",
                    Result = new
                    {
                        SectionId = sectionId,
                        IsActive = request.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar el estado de la sección {SectionId}.",
                    sectionId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al actualizar el estado de la sección.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Agrega un producto a una sección en una posición determinada.
        /// Los productos ubicados desde esa posición en adelante
        /// se desplazan una posición.
        /// </summary>
        [HttpPost("{solutionCenterId:long}/sections/{sectionId:long}/products")]
        public async Task<IActionResult> AddProductToSection(
            long solutionCenterId,
            long sectionId,
            [FromBody] AddSectionProductDto request)
        {
            try
            {
                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la bodega debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la sección debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información del producto no es válida.",
                        Result = new { }
                    });
                }

                if (request.ProductId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador del producto debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request.Position <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La posición debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (string.IsNullOrWhiteSpace(request.CreatedBy))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que crea el registro es obligatorio.",
                        Result = new { }
                    });
                }

                var solutionCenterProductId =
                    await _solutionCenterApplication
                        .AddProductToSection(
                            solutionCenterId,
                            sectionId,
                            request);

                if (solutionCenterProductId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo agregar el producto a la sección. " +
                            "Verifique que la bodega, la sección y el producto existan, " +
                            "que la posición sea válida y que no exista otro producto " +
                            "con el mismo nombre y unidad de medida dentro de la sección.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Producto agregado a la sección correctamente.",
                    Result = new
                    {
                        SolutionCenterProductId = solutionCenterProductId,
                        SolutionCenterId = solutionCenterId,
                        SectionId = sectionId,
                        ProductId = request.ProductId,
                        Position = request.Position
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al agregar el producto {ProductId} a la sección {SectionId} de la bodega {SolutionCenterId}.",
                    request?.ProductId,
                    sectionId,
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al agregar el producto a la sección.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Intercambia la posición de un producto con el producto
        /// que actualmente ocupa la nueva posición.
        /// </summary>
        [HttpPatch(
            "{solutionCenterId:long}/sections/{sectionId:long}/products/{solutionCenterProductId:long}/order")]
        public async Task<IActionResult> UpdateProductOrder(
            long solutionCenterId,
            long sectionId,
            long solutionCenterProductId,
            [FromBody] UpdateProductOrderDto request)
        {
            try
            {
                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la bodega debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la sección debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (solutionCenterProductId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador del producto asociado debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null ||
                    request.NewPosition <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La nueva posición debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                var updated =
                    await _solutionCenterApplication
                        .UpdateProductOrder(
                            solutionCenterId,
                            sectionId,
                            solutionCenterProductId,
                            request.NewPosition);

                if (!updated)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo actualizar el orden. " +
                            "Verifique que la bodega, la sección, el producto asociado " +
                            "y la posición solicitada existan.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Orden del producto actualizado correctamente.",
                    Result = new
                    {
                        SolutionCenterProductId =
                            solutionCenterProductId,

                        NewPosition =
                            request.NewPosition
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar el orden del producto asociado {SolutionCenterProductId} " +
                    "en la sección {SectionId} de la bodega {SolutionCenterId}.",
                    solutionCenterProductId,
                    sectionId,
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al actualizar el orden del producto.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Elimina un producto de una sección y reorganiza
        /// automáticamente las posiciones posteriores.
        /// </summary>
        [HttpDelete(
            "{solutionCenterId:long}/sections/{sectionId:long}/products/{solutionCenterProductId:long}")]
        public async Task<IActionResult> DeleteProductFromSection(
            long solutionCenterId,
            long sectionId,
            long solutionCenterProductId)
        {
            try
            {
                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la bodega debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la sección debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (solutionCenterProductId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador del producto asociado debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                var deleted =
                    await _solutionCenterApplication
                        .DeleteProductFromSection(
                            solutionCenterId,
                            sectionId,
                            solutionCenterProductId);

                if (!deleted)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo eliminar el producto de la sección. " +
                            "Verifique que la relación exista y que no sea " +
                            "el último producto de la sección.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Producto eliminado de la sección correctamente.",
                    Result = new
                    {
                        SolutionCenterProductId =
                            solutionCenterProductId,

                        SolutionCenterId =
                            solutionCenterId,

                        SectionId =
                            sectionId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar el producto asociado {SolutionCenterProductId} " +
                    "de la sección {SectionId} de la bodega {SolutionCenterId}.",
                    solutionCenterProductId,
                    sectionId,
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al eliminar el producto de la sección.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Actualiza el código y el nombre de una bodega.
        /// </summary>
        [HttpPatch("{solutionCenterId:long}")]
        public async Task<IActionResult> UpdateSolutionCenter(
            long solutionCenterId,
            [FromBody] UpdateSolutionCenterDto request)
        {
            try
            {
                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la bodega debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información enviada no es válida.",
                        Result = new { }
                    });
                }

                if (string.IsNullOrWhiteSpace(
                        request.SolutionCenterCode) ||
                    string.IsNullOrWhiteSpace(
                        request.SolutionCenterName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El código y el nombre son obligatorios.",
                        Result = new { }
                    });
                }

                var updated =
                    await _solutionCenterApplication
                        .UpdateSolutionCenter(
                            solutionCenterId,
                            request);

                if (!updated)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo actualizar la bodega. " +
                            "Verifique que exista y que el código o nombre " +
                            "no estén registrados.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Bodega actualizada correctamente.",
                    Result = new
                    {
                        SolutionCenterId =
                            solutionCenterId,

                        SolutionCenterCode =
                            request.SolutionCenterCode
                                .Trim()
                                .ToUpperInvariant(),

                        SolutionCenterName =
                            request.SolutionCenterName
                                .Trim()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar la bodega {SolutionCenterId}.",
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al actualizar la bodega.",
                    Result = new { }
                });
            }
        }
    }
}