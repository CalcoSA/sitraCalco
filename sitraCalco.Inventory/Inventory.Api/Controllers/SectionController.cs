using FluentValidation;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SectionController : ControllerBase
    {
        private readonly ISectionApplication _sectionApplication;
        private readonly ILogApplication _logApplication;
        private readonly IValidator<CreateSectionDto> _createSectionValidator;
        private readonly IValidator<UpdateSectionDto> _updateSectionValidator;
        private readonly ILogger<SectionController> _logger;

        public SectionController(
            ISectionApplication sectionApplication,
            ILogApplication logApplication,
            IValidator<CreateSectionDto> createSectionValidator,
            IValidator<UpdateSectionDto> updateSectionValidator,
            ILogger<SectionController> logger)
        {
            _sectionApplication = sectionApplication;
            _logApplication = logApplication;
            _createSectionValidator = createSectionValidator;
            _updateSectionValidator = updateSectionValidator;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las secciones registradas.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var sections =
                    (await _sectionApplication.GetAll())
                    .ToList();

                if (!sections.Any())
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No hay secciones registradas.",
                        Result = sections
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Secciones consultadas correctamente.",
                    Result = sections
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar las secciones.");

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar las secciones.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Obtiene una sección por su identificador.
        /// </summary>
        [HttpGet("{sectionId:long}")]
        public async Task<IActionResult> GetById(
            long sectionId)
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

                var section =
                    await _sectionApplication
                        .GetById(sectionId);

                if (section is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La sección no existe.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Sección consultada correctamente.",
                    Result = section
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar la sección {SectionId}.",
                    sectionId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar la sección.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Crea una nueva sección.
        /// La sección se crea activa por defecto.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSectionDto request,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
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

                var validation =
                    await _createSectionValidator
                        .ValidateAsync(request);

                if (!validation.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La solicitud no es válida.",
                        Result =
                            validation.Errors
                                .Select(error =>
                                    error.ErrorMessage)
                    });
                }

                var sectionId =
                    await _sectionApplication
                        .Create(request);

                if (sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo crear la sección. " +
                            "Verifique que el nombre no esté registrado.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Crear",
                        Module = "ConfiguracionSecciones",
                        Description =
                            $"Se creó la sección {request.SectionName.Trim()}.",
                        UserName = userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Sección creada correctamente.",
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
                    "Error al crear una sección.");

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al crear la sección.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Actualiza el nombre y el estado de una sección.
        /// </summary>
        [HttpPut("{sectionId:long}")]
        public async Task<IActionResult> Update(
            long sectionId,
            [FromBody] UpdateSectionDto request,
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

                var validation =
                    await _updateSectionValidator
                        .ValidateAsync(request);

                if (!validation.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La solicitud no es válida.",
                        Result =
                            validation.Errors
                                .Select(error =>
                                    error.ErrorMessage)
                    });
                }

                var currentSection =
                    await _sectionApplication
                        .GetById(sectionId);

                if (currentSection is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La sección no existe.",
                        Result = new { }
                    });
                }

                var updated =
                    await _sectionApplication
                        .Update(
                            sectionId,
                            request);

                if (!updated)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo actualizar la sección. " +
                            "Verifique que el nombre no esté registrado.",
                        Result = new { }
                    });
                }

                var changes = new List<string>();

                if (!string.Equals(
                    currentSection.SectionName,
                    request.SectionName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add(
                        $"nombre de {currentSection.SectionName} " +
                        $"a {request.SectionName.Trim()}");
                }

                if (currentSection.IsActive !=
                    request.IsActive)
                {
                    changes.Add(
                        request.IsActive
                            ? "estado a activo"
                            : "estado a inactivo");
                }

                var description =
                    changes.Any()
                        ? string.Join(" y ", changes)
                        : "sin cambios";

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Actualizar",
                        Module = "ConfiguracionSecciones",
                        Description =
                            $"Se actualizó la sección " +
                            $"{request.SectionName.Trim()}: " +
                            $"{description}.",
                        UserName = userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Sección actualizada correctamente.",
                    Result = new
                    {
                        SectionId = sectionId,
                        SectionName =
                            request.SectionName.Trim(),
                        IsActive =
                            request.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar la sección {SectionId}.",
                    sectionId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al actualizar la sección.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Elimina una sección si no tiene registros asociados.
        /// </summary>
        [HttpDelete("{sectionId:long}")]
        public async Task<IActionResult> Delete(
            long sectionId,
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

                var currentSection =
                    await _sectionApplication
                        .GetById(sectionId);

                if (currentSection is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La sección no existe.",
                        Result = new { }
                    });
                }

                var deleted =
                    await _sectionApplication
                        .Delete(sectionId);

                if (!deleted)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se puede eliminar la sección porque tiene registros asociados.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Eliminar",
                        Module = "ConfiguracionSecciones",
                        Description =
                            $"Se eliminó la sección " +
                            $"{currentSection.SectionName}.",
                        UserName = userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Sección eliminada correctamente.",
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
                    "Error al eliminar la sección {SectionId}.",
                    sectionId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al eliminar la sección.",
                    Result = new { }
                });
            }
        }
    }
}