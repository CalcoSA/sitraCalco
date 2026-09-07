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
    }
}