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
        private readonly ISolutionCenterApplication
            _solutionCenterApplication;

        private readonly ILogger<SolutionCenterController>
            _logger;

        public SolutionCenterController(
            ISolutionCenterApplication solutionCenterApplication,
            ILogger<SolutionCenterController> logger)
        {
            _solutionCenterApplication = solutionCenterApplication;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene los tipos de centros de soluciones.
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
                            "No hay tipos de centros de soluciones registrados.",
                        Result = types
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Tipos de centros de soluciones consultados correctamente.",
                    Result = types
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar los tipos de centros de soluciones.");

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar los tipos de centros de soluciones.",
                    Result = new { }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSolutionCenter(
    [FromBody] CreateSolutionCenterDto request)
        {
            try
            {
                if (request is null ||
                    request.SolutionCenterTypeId <= 0 ||
                    string.IsNullOrWhiteSpace(
                        request.SolutionCenterCode) ||
                    string.IsNullOrWhiteSpace(
                        request.SolutionCenterName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La información del centro de soluciones no es válida.",
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
                            "No se pudo crear el centro de soluciones. Verifique el tipo y que el código no exista.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Centro de soluciones creado correctamente.",
                    Result = new
                    {
                        SolutionCenterId =
                            solutionCenterId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear el centro de soluciones.");

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al crear el centro de soluciones.",
                    Result = new { }
                });
            }
        }

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
                        Message = "El identificador del centro de soluciones debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La información de la sección no es válida.",
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
                            "Verifique que el centro exista, que el nombre de la sección no esté repetido, " +
                            "que los productos sean válidos y que no existan combinaciones repetidas de referencia y unidad de medida.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Configuración de la sección guardada correctamente.",
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
                    "Error al guardar la configuración de la sección para el centro {SolutionCenterId}.",
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al guardar la configuración de la sección.",
                    Result = new { }
                });
            }
        }

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
                        Message = "El rol es obligatorio.",
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
                            "No hay centros de soluciones disponibles para el rol.",
                        Result = solutionCenters
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Centros de soluciones consultados correctamente.",
                    Result = solutionCenters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar centros de soluciones para el rol {Role}.",
                    role);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar los centros de soluciones.",
                    Result = new { }
                });
            }
        }

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
                            "El identificador del centro de soluciones debe ser mayor a cero.",
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
                            "El centro de soluciones no existe.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Centro de soluciones consultado correctamente.",
                    Result = solutionCenter
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar el centro de soluciones {SolutionCenterId}.",
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar el centro de soluciones.",
                    Result = new { }
                });
            }

        }

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
                            "El identificador del centro de soluciones debe ser mayor a cero.",
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
                            "El centro de soluciones no existe o no se pudo actualizar.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        request.IsActive
                            ? "Centro de soluciones activado correctamente."
                            : "Centro de soluciones inactivado correctamente.",
                    Result = new
                    {
                        SolutionCenterId =
                            solutionCenterId,

                        IsActive =
                            request.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar el estado del centro de soluciones {SolutionCenterId}.",
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al actualizar el estado del centro de soluciones.",
                    Result = new { }
                });
            }
        }
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
                        Message = "El identificador del centro de soluciones debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El identificador de la sección debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La información del producto no es válida.",
                        Result = new { }
                    });
                }

                if (request.ProductId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El identificador del producto debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request.Position <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La posición debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (string.IsNullOrWhiteSpace(request.CreatedBy))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El usuario que crea el registro es obligatorio.",
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
                            "Verifique que el centro, la sección y el producto existan, " +
                            "que la posición sea válida y que no exista otro producto " +
                            "con el mismo nombre y unidad de medida dentro de la sección.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Producto agregado a la sección correctamente.",
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
                    "Error al agregar el producto {ProductId} a la sección {SectionId} del centro {SolutionCenterId}.",
                    request?.ProductId,
                    sectionId,
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al agregar el producto a la sección.",
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
                            "El identificador del centro de soluciones debe ser mayor a cero.",
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
                            "Verifique que el centro, la sección, el producto asociado " +
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
                    "Error al cambiar el orden del producto asociado {SolutionCenterProductId} en la sección {SectionId}.",
                    solutionCenterProductId,
                    sectionId);

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
                            "El identificador del centro de soluciones debe ser mayor a cero.",
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
                    "de la sección {SectionId} del centro {SolutionCenterId}.",
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
    }
}