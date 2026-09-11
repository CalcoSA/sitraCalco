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
        private readonly ILogApplication _logApplication;
        private readonly ILogger<SolutionCenterController> _logger;

        public SolutionCenterController(
            ISolutionCenterApplication solutionCenterApplication,
            ILogApplication logApplication,
            ILogger<SolutionCenterController> logger)
        {
            _solutionCenterApplication = solutionCenterApplication;
            _logApplication = logApplication;
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
            [FromBody] CreateSolutionCenterDto request,
            [FromHeader(Name = "X-User")] string userName)
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

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que ejecuta la operación es obligatorio.",
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

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Crear",
                        Module = "ConfiguracionBodegas",
                        Description =
                            $"Se creó la bodega {request.SolutionCenterName.Trim()} " +
                            $"con código {request.SolutionCenterCode.Trim().ToUpperInvariant()}.",
                        UserName = userName.Trim()
                    });

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
            [FromBody] CreateSectionConfigurationDto request,
            [FromHeader(Name = "X-User")] string userName)
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

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que ejecuta la operación es obligatorio.",
                        Result = new { }
                    });
                }

                var sectionId =
                    await _solutionCenterApplication
                        .CreateSectionConfiguration(
                            solutionCenterId,
                            request,
                            userName);

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

                var solutionCenter =
                    await _solutionCenterApplication
                        .GetSolutionCenterById(solutionCenterId);

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Crear",
                        Module = "ConfiguracionBodegas",
                        Description =
                            $"Se creó la sección {request.SectionName.Trim()} " +
                            $"en la bodega " +
                            $"{solutionCenter?.SolutionCenterName ?? solutionCenterId.ToString()}.",
                        UserName = userName.Trim()
                    });

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
            [FromBody] UpdateStatusDto request,
            [FromHeader(Name = "X-User")] string userName)
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

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que ejecuta la operación es obligatorio.",
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

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Actualizar",
                        Module = "ConfiguracionBodegas",
                        Description =
                            request.IsActive
                                ? $"Se activó la bodega {solutionCenter.SolutionCenterName} " +
                                  $"con código {solutionCenter.SolutionCenterCode}."
                                : $"Se inactivó la bodega {solutionCenter.SolutionCenterName} " +
                                  $"con código {solutionCenter.SolutionCenterCode}.",
                        UserName = userName.Trim()
                    });

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
            [FromBody] UpdateStatusDto request,
            [FromHeader(Name = "X-User")] string userName)
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

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que ejecuta la operación es obligatorio.",
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

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Actualizar",
                        Module = "ConfiguracionBodegas",
                        Description =
                            request.IsActive
                                ? $"Se activó la sección con ID {sectionId}."
                                : $"Se inactivó la sección con ID {sectionId}.",
                        UserName = userName.Trim()
                    });

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
        /// </summary>
        [HttpPost("{solutionCenterId:long}/sections/{sectionId:long}/products")]
        public async Task<IActionResult> AddProductToSection(
            long solutionCenterId,
            long sectionId,
            [FromBody] AddSectionProductDto request,
            [FromHeader(Name = "X-User")] string userName)
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

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que ejecuta la operación es obligatorio.",
                        Result = new { }
                    });
                }

                // Mientras AddSectionProductDto conserve CreatedBy,
                // la fuente real será X-User.
                request.CreatedBy = userName.Trim();

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

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Crear",
                        Module = "ConfiguracionBodegas",
                        Description =
                            $"Se agregó el producto con ID {request.ProductId} " +
                            $"a la sección con ID {sectionId} " +
                            $"en la posición {request.Position}.",
                        UserName = userName.Trim()
                    });

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
            [FromBody] UpdateProductOrderDto request,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (solutionCenterId <= 0 ||
                    sectionId <= 0 ||
                    solutionCenterProductId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "Los identificadores enviados no son válidos.",
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

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que ejecuta la operación es obligatorio.",
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

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Actualizar",
                        Module = "ConfiguracionBodegas",
                        Description =
                            $"Se cambió el orden del producto asociado " +
                            $"con ID {solutionCenterProductId} " +
                            $"en la sección con ID {sectionId} " +
                            $"a la posición {request.NewPosition}.",
                        UserName = userName.Trim()
                    });

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
                    "Error al cambiar el orden del producto asociado {SolutionCenterProductId}.",
                    solutionCenterProductId);

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
            long solutionCenterProductId,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (solutionCenterId <= 0 ||
                    sectionId <= 0 ||
                    solutionCenterProductId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "Los identificadores enviados no son válidos.",
                        Result = new { }
                    });
                }

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que ejecuta la operación es obligatorio.",
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

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Eliminar",
                        Module = "ConfiguracionBodegas",
                        Description =
                            $"Se eliminó el producto asociado con ID " +
                            $"{solutionCenterProductId} " +
                            $"de la sección con ID {sectionId}.",
                        UserName = userName.Trim()
                    });

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
                    "Error al eliminar el producto asociado {SolutionCenterProductId}.",
                    solutionCenterProductId);

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
            [FromBody] UpdateSolutionCenterDto request,
            [FromHeader(Name = "X-User")] string userName)
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

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El usuario que ejecuta la operación es obligatorio.",
                        Result = new { }
                    });
                }

                var currentSolutionCenter =
                    await _solutionCenterApplication
                        .GetSolutionCenterById(
                            solutionCenterId);

                if (currentSolutionCenter is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La bodega no existe.",
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

                var newCode =
                    request.SolutionCenterCode
                        .Trim()
                        .ToUpperInvariant();

                var newName =
                    request.SolutionCenterName
                        .Trim();

                var changes = new List<string>();

                if (!string.Equals(
                    currentSolutionCenter.SolutionCenterCode,
                    newCode,
                    StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add(
                        $"código de {currentSolutionCenter.SolutionCenterCode} " +
                        $"a {newCode}");
                }

                if (!string.Equals(
                    currentSolutionCenter.SolutionCenterName,
                    newName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add(
                        $"nombre de {currentSolutionCenter.SolutionCenterName} " +
                        $"a {newName}");
                }

                var changeDescription =
                    changes.Any()
                        ? string.Join(" y ", changes)
                        : "sin cambios en nombre o código";

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Actualizar",
                        Module = "ConfiguracionBodegas",
                        Description =
                            $"Se actualizó la bodega {newName}: " +
                            $"{changeDescription}.",
                        UserName = userName.Trim()
                    });

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
                            newCode,
                        SolutionCenterName =
                            newName
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