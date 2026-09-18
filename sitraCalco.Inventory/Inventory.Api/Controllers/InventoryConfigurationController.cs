using FluentValidation;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryConfigurationController : ControllerBase
    {
        private readonly IInventoryConfigurationApplication
            _inventoryConfigurationApplication;

        private readonly ILogApplication
            _logApplication;

        private readonly IValidator<CreateInventoryConfigurationDto>
            _createInventoryConfigurationValidator;

        private readonly IValidator<CreateInventoryConfigurationAssignmentsDto>
             _createAssignmentsValidator;

        private readonly ILogger<InventoryConfigurationController>
            _logger;

        private readonly IValidator<UpdateInventoryConfigurationAssignmentStatusDto>
            _updateAssignmentStatusValidator;
        private readonly IValidator<UpdateInventoryConfigurationDto>
            _updateInventoryConfigurationValidator;
        private readonly IValidator<AddInventoryConfigurationDaysDto>
            _addInventoryConfigurationDaysValidator;

        public InventoryConfigurationController(
            IInventoryConfigurationApplication inventoryConfigurationApplication,
            ILogApplication logApplication,
            IValidator<CreateInventoryConfigurationDto>
                createInventoryConfigurationValidator,
            IValidator<CreateInventoryConfigurationAssignmentsDto>
                createAssignmentsValidator,
            IValidator<UpdateInventoryConfigurationAssignmentStatusDto>
                updateAssignmentStatusValidator,
            IValidator<UpdateInventoryConfigurationDto>
                updateInventoryConfigurationValidator,
            IValidator<AddInventoryConfigurationDaysDto>
                addInventoryConfigurationDaysValidator,
            ILogger<InventoryConfigurationController> logger)            
        {
            _inventoryConfigurationApplication =
                inventoryConfigurationApplication;

            _logApplication =
                logApplication;

            _createInventoryConfigurationValidator =
                createInventoryConfigurationValidator;

            _createAssignmentsValidator =
                createAssignmentsValidator;

            _updateAssignmentStatusValidator =
                updateAssignmentStatusValidator;

            _updateInventoryConfigurationValidator =
                updateInventoryConfigurationValidator;

            _logger =
                logger;

            _addInventoryConfigurationDaysValidator =
                addInventoryConfigurationDaysValidator;
        }

        /// <summary>
        /// Crea una nueva configuración de inventario.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateInventoryConfigurationDto request,
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
                    await _createInventoryConfigurationValidator
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

                var inventoryConfigurationId =
                    await _inventoryConfigurationApplication
                        .Create(request);

                if (inventoryConfigurationId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo crear la configuración de inventario. " +
                            "Verifique que el nombre no esté registrado " +
                            "y que el rango de fechas sea válido.",
                        Result = new { }
                    });
                }

                var dateDescription =
                    !request.StartDate.HasValue &&
                    !request.EndDate.HasValue
                        ? "Sin rango de fechas definido."
                        : $"Fecha de inicio: " +
                          $"{(request.StartDate.HasValue
                              ? request.StartDate.Value.ToString("yyyy-MM-dd")
                              : "sin definir")}. " +
                          $"Fecha de fin: " +
                          $"{(request.EndDate.HasValue
                              ? request.EndDate.Value.ToString("yyyy-MM-dd")
                              : "sin definir")}.";

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Crear",

                        Module =
                            "ConfiguracionInventarios",

                        Description =
                            $"Se creó la configuración de inventario " +
                            $"{request.InventoryConfigurationName.Trim()}. " +
                            $"{dateDescription}",

                        UserName =
                            userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Configuración de inventario creada correctamente.",
                    Result = new
                    {
                        InventoryConfigurationId =
                            inventoryConfigurationId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear la configuración de inventario.");

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al crear la configuración de inventario.",
                    Result = new { }
                });
            }
        }

        /// <summary>
        /// Asocia bodegas o puntos de venta y secciones
        /// a una configuración de inventario.
        /// </summary>
        [HttpPost("{inventoryConfigurationId:long}/assignments")]
        public async Task<IActionResult> CreateAssignments(
            long inventoryConfigurationId,
            [FromBody] CreateInventoryConfigurationAssignmentsDto request,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la configuración de inventario debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información de las asignaciones no es válida.",
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
                    await _createAssignmentsValidator
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

                var createdAssignments =
                    await _inventoryConfigurationApplication
                        .CreateAssignments(
                            inventoryConfigurationId,
                            request);

                if (createdAssignments <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudieron guardar las asignaciones. " +
                            "Verifique que la configuración, las bodegas o puntos de venta y las secciones existan, " +
                            "que cada sección pertenezca al centro indicado " +
                            "y que las combinaciones no estén registradas.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Crear",
                        Module = "ConfiguracionInventarios",
                        Description =
                            $"Se agregaron {createdAssignments} asignaciones " +
                            $"a la configuración de inventario con ID " +
                            $"{inventoryConfigurationId}.",
                        UserName = userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Asignaciones de la configuración de inventario guardadas correctamente.",
                    Result = new
                    {
                        InventoryConfigurationId =
                            inventoryConfigurationId,

                        AssignmentsCreated =
                            createdAssignments
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al guardar las asignaciones de la configuración de inventario {InventoryConfigurationId}.",
                    inventoryConfigurationId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al guardar las asignaciones de la configuración de inventario.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Obtiene las opciones necesarias para configurar inventarios.
        /// 1: Bodegas.
        /// 2: Puntos de Venta.
        /// 3: Secciones exclusivas de Puntos de Venta.
        /// </summary>
        [HttpGet("options")]
        public async Task<IActionResult> GetOptions(
            [FromQuery] int type)
        {
            try
            {
                if (type < 1 || type > 3)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El tipo debe ser 1, 2 o 3.",
                        Result = new { }
                    });
                }

                var options =
                    await _inventoryConfigurationApplication
                        .GetOptions(type);

                if (options is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El tipo enviado no es válido.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        type switch
                        {
                            1 => "Bodegas consultadas correctamente.",
                            2 => "Puntos de venta consultados correctamente.",
                            3 => "Secciones consultadas correctamente.",
                            _ => "Opciones consultadas correctamente."
                        },
                    Result = options
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar las opciones de configuración de inventarios. Tipo {Type}.",
                    type);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar las opciones de configuración de inventarios.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Obtiene una bodega o punto de venta
        /// junto con todas sus secciones asociadas.
        /// </summary>
        [HttpGet("solution-center/{solutionCenterId:long}")]
        public async Task<IActionResult> GetSolutionCenterWithSections(
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
                            "El identificador de la bodega o punto de venta debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                var solutionCenter =
                    await _inventoryConfigurationApplication
                        .GetSolutionCenterWithSections(
                            solutionCenterId);

                if (solutionCenter is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La bodega o punto de venta no existe.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Bodega o punto de venta consultado correctamente.",
                    Result = solutionCenter
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar la bodega o punto de venta {SolutionCenterId}.",
                    solutionCenterId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar la bodega o punto de venta.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Obtiene una sección junto con todos los
        /// Puntos de Venta asociados.
        /// </summary>
        [HttpGet("section/{sectionId:long}")]
        public async Task<IActionResult> GetSectionWithPointOfSales(
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
                    await _inventoryConfigurationApplication
                        .GetSectionWithPointOfSales(
                            sectionId);

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
                        "Sección y puntos de venta consultados correctamente.",
                    Result = section
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar la sección {SectionId} y sus puntos de venta.",
                    sectionId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar la sección y sus puntos de venta.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Obtiene todas las configuraciones de inventario
        /// asociadas a una bodega o punto de venta,
        /// aplicando permisos según el rol.
        /// </summary>
        [HttpGet(
            "solution-center/{solutionCenterId:long}/configurations")]
        public async Task<IActionResult>
            GetInventoryConfigurationsBySolutionCenterId(
                long solutionCenterId,
                [FromQuery] string role)
        {
            try
            {
                if (solutionCenterId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la bodega o punto de venta debe ser mayor a cero.",
                        Result = new { }
                    });
                }

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

                var result =
                    await _inventoryConfigurationApplication
                        .GetInventoryConfigurationsBySolutionCenterId(
                            solutionCenterId,
                            role);

                if (!result.IsValidRole)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El rol enviado no es válido.",
                        Result = new { }
                    });
                }

                if (!result.IsAllowed)
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ResponseApi
                        {
                            IsSuccess = false,
                            Message =
                                "El rol no tiene permisos para consultar este punto de venta.",
                            Result = new { }
                        });
                }

                if (result.Data is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La bodega o punto de venta no existe.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Configuraciones de inventario consultadas correctamente.",
                    Result = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar las configuraciones de inventario " +
                    "del Solution Center {SolutionCenterId} para el rol {Role}.",
                    solutionCenterId,
                    role);

                return StatusCode(
                    500,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "Ocurrió un error al consultar las configuraciones de inventario.",
                        Result = new { }
                    });
            }
        }
        /// <summary>
        /// Obtiene una configuración de inventario por su identificador,
        /// incluyendo sus días, bodegas o puntos de venta y secciones,
        /// aplicando visibilidad según el rol.
        /// </summary>
        [HttpGet("{inventoryConfigurationId:long}/details")]
        public async Task<IActionResult> GetInventoryConfigurationById(
            long inventoryConfigurationId,
            [FromQuery] string role)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la configuración de inventario debe ser mayor a cero.",
                        Result = new { }
                    });
                }

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

                var result =
                    await _inventoryConfigurationApplication
                        .GetInventoryConfigurationById(
                            inventoryConfigurationId,
                            role);

                if (!result.IsValidRole)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El rol enviado no es válido.",
                        Result = new { }
                    });
                }

                if (result.Data is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La configuración de inventario no existe.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Configuración de inventario consultada correctamente.",
                    Result = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar la configuración de inventario {InventoryConfigurationId} para el rol {Role}.",
                    inventoryConfigurationId,
                    role);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al consultar la configuración de inventario.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Actualiza el estado de una sección dentro
        /// de una configuración de inventario.
        /// </summary>
        [HttpPatch(
            "{inventoryConfigurationId:long}/solution-centers/{solutionCenterId:long}/sections/{sectionId:long}/status")]
        public async Task<IActionResult> UpdateAssignmentStatus(
            long inventoryConfigurationId,
            long solutionCenterId,
            long sectionId,
            [FromBody] UpdateInventoryConfigurationAssignmentStatusDto request,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (inventoryConfigurationId <= 0 ||
                    solutionCenterId <= 0 ||
                    sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "Los identificadores enviados deben ser mayores a cero.",
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

                var validation =
                    await _updateAssignmentStatusValidator
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

                var updated =
                    await _inventoryConfigurationApplication
                        .UpdateAssignmentStatus(
                            inventoryConfigurationId,
                            solutionCenterId,
                            sectionId,
                            request.IsActive!.Value);

                if (!updated)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La asociación entre la configuración de inventario, " +
                            "la bodega o punto de venta y la sección no existe.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Actualizar",
                        Module = "ConfiguracionInventarios",
                        Description =
                            request.IsActive.Value
                                ? $"Se activó la sección con ID {sectionId} " +
                                  $"para el centro con ID {solutionCenterId} " +
                                  $"en la configuración de inventario con ID " +
                                  $"{inventoryConfigurationId}."
                                : $"Se inactivó la sección con ID {sectionId} " +
                                  $"para el centro con ID {solutionCenterId} " +
                                  $"en la configuración de inventario con ID " +
                                  $"{inventoryConfigurationId}.",
                        UserName =
                            userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        request.IsActive.Value
                            ? "Asignación activada correctamente."
                            : "Asignación inactivada correctamente.",
                    Result = new
                    {
                        InventoryConfigurationId =
                            inventoryConfigurationId,

                        SolutionCenterId =
                            solutionCenterId,

                        SectionId =
                            sectionId,

                        IsActive =
                            request.IsActive.Value
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar el estado de la asociación. " +
                    "Configuración {InventoryConfigurationId}, " +
                    "Solution Center {SolutionCenterId}, " +
                    "Sección {SectionId}.",
                    inventoryConfigurationId,
                    solutionCenterId,
                    sectionId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al actualizar el estado de la asignación.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Actualiza el nombre y el rango de fechas
        /// de una configuración de inventario.
        /// </summary>
        [HttpPatch("{inventoryConfigurationId:long}")]
        public async Task<IActionResult> UpdateInventoryConfiguration(
            long inventoryConfigurationId,
            [FromBody] UpdateInventoryConfigurationDto request,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la configuración de inventario debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información de la configuración de inventario no es válida.",
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
                    await _updateInventoryConfigurationValidator
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

                var updated =
                    await _inventoryConfigurationApplication
                        .UpdateInventoryConfiguration(
                            inventoryConfigurationId,
                            request);

                if (!updated)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudo actualizar la configuración de inventario. " +
                            "Verifique que exista, que el nombre no esté registrado " +
                            "y que el rango de fechas sea válido.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Actualizar",
                        Module = "ConfiguracionInventarios",
                        Description =
                            $"Se actualizó la configuración de inventario " +
                            $"{request.InventoryConfigurationName.Trim()} " +
                            $"con ID {inventoryConfigurationId}. " +
                            $"Fecha de inicio: " +
                            $"{(request.StartDate.HasValue
                                ? request.StartDate.Value.ToString("yyyy-MM-dd")
                                : "sin definir")}. " +
                            $"Fecha de fin: " +
                            $"{(request.EndDate.HasValue
                                ? request.EndDate.Value.ToString("yyyy-MM-dd")
                                : "sin definir")}.",
                        UserName =
                            userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Configuración de inventario actualizada correctamente.",
                    Result = new
                    {
                        InventoryConfigurationId =
                            inventoryConfigurationId,

                        InventoryConfigurationName =
                            request.InventoryConfigurationName.Trim(),

                        StartDate =
                            request.StartDate?.Date,

                        EndDate =
                            request.EndDate?.Date
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar la configuración de inventario {InventoryConfigurationId}.",
                    inventoryConfigurationId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al actualizar la configuración de inventario.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Agrega uno o varios días a una configuración de inventario.
        /// </summary>
        [HttpPost("{inventoryConfigurationId:long}/days")]
        public async Task<IActionResult> AddDays(
            long inventoryConfigurationId,
            [FromBody] AddInventoryConfigurationDaysDto request,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la configuración de inventario debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La información de los días no es válida.",
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
                    await _addInventoryConfigurationDaysValidator
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

                var daysCreated =
                    await _inventoryConfigurationApplication
                        .AddDays(
                            inventoryConfigurationId,
                            request);

                if (daysCreated <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se pudieron agregar los días. " +
                            "Verifique que la configuración exista " +
                            "y que los días no estén registrados.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Crear",
                        Module = "ConfiguracionInventarios",
                        Description =
                            $"Se agregaron los días " +
                            $"{string.Join(", ", request.Days)} " +
                            $"a la configuración de inventario con ID " +
                            $"{inventoryConfigurationId}.",
                        UserName =
                            userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Días agregados correctamente.",
                    Result = new
                    {
                        InventoryConfigurationId =
                            inventoryConfigurationId,

                        DaysCreated =
                            daysCreated,

                        Days =
                            request.Days
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al agregar días a la configuración de inventario {InventoryConfigurationId}.",
                    inventoryConfigurationId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al agregar los días a la configuración de inventario.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Elimina un día específico de una
        /// configuración de inventario.
        /// </summary>
        [HttpDelete("{inventoryConfigurationId:long}/days/{dayOfWeek}")]
        public async Task<IActionResult> DeleteDay(
            long inventoryConfigurationId,
            string dayOfWeek,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El identificador de la configuración de inventario debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (string.IsNullOrWhiteSpace(dayOfWeek))
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "El día es obligatorio.",
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
                    await _inventoryConfigurationApplication
                        .DeleteDay(
                            inventoryConfigurationId,
                            dayOfWeek);

                if (!deleted)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se encontró el día indicado en la configuración de inventario.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Eliminar",
                        Module = "ConfiguracionInventarios",
                        Description =
                            $"Se eliminó el día {dayOfWeek.Trim()} " +
                            $"de la configuración de inventario con ID " +
                            $"{inventoryConfigurationId}.",
                        UserName =
                            userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Día eliminado correctamente.",
                    Result = new
                    {
                        InventoryConfigurationId =
                            inventoryConfigurationId,

                        Day =
                            dayOfWeek.Trim()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar el día {DayOfWeek} " +
                    "de la configuración de inventario {InventoryConfigurationId}.",
                    dayOfWeek,
                    inventoryConfigurationId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al eliminar el día de la configuración de inventario.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Elimina una asociación específica entre
        /// configuración de inventario, bodega o punto de venta y sección.
        /// </summary>
        [HttpDelete(
            "{inventoryConfigurationId:long}/solution-centers/{solutionCenterId:long}/sections/{sectionId:long}")]
        public async Task<IActionResult> DeleteAssignment(
            long inventoryConfigurationId,
            long solutionCenterId,
            long sectionId,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (inventoryConfigurationId <= 0 ||
                    solutionCenterId <= 0 ||
                    sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "Los identificadores enviados deben ser mayores a cero.",
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
                    await _inventoryConfigurationApplication
                        .DeleteAssignment(
                            inventoryConfigurationId,
                            solutionCenterId,
                            sectionId);

                if (!deleted)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "La asociación indicada no existe.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Eliminar",
                        Module = "ConfiguracionInventarios",
                        Description =
                            $"Se eliminó la asociación de la sección con ID " +
                            $"{sectionId} del centro con ID {solutionCenterId} " +
                            $"en la configuración de inventario con ID " +
                            $"{inventoryConfigurationId}.",
                        UserName =
                            userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Asignación eliminada correctamente.",
                    Result = new
                    {
                        InventoryConfigurationId =
                            inventoryConfigurationId,

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
                    "Error al eliminar la asociación. " +
                    "Configuración {InventoryConfigurationId}, " +
                    "Solution Center {SolutionCenterId}, " +
                    "Sección {SectionId}.",
                    inventoryConfigurationId,
                    solutionCenterId,
                    sectionId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al eliminar la asignación.",
                    Result = new { }
                });
            }
        }
        /// <summary>
        /// Elimina una sección de todos los Solution Centers
        /// asociados dentro de una configuración de inventario.
        /// </summary>
        [HttpDelete(
            "{inventoryConfigurationId:long}/sections/{sectionId:long}")]
        public async Task<IActionResult> DeleteAssignmentsBySection(
            long inventoryConfigurationId,
            long sectionId,
            [FromHeader(Name = "X-User")] string userName)
        {
            try
            {
                if (inventoryConfigurationId <= 0 ||
                    sectionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "Los identificadores enviados deben ser mayores a cero.",
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

                var deletedAssignments =
                    await _inventoryConfigurationApplication
                        .DeleteAssignmentsBySection(
                            inventoryConfigurationId,
                            sectionId);

                if (deletedAssignments <= 0)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message =
                            "No se encontraron asociaciones de la sección " +
                            "en la configuración de inventario indicada.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(
                    new CreateLogDto
                    {
                        Action = "Eliminar",
                        Module = "ConfiguracionInventarios",
                        Description =
                            $"Se eliminó la sección con ID {sectionId} " +
                            $"de {deletedAssignments} asociación(es) " +
                            $"de la configuración de inventario con ID " +
                            $"{inventoryConfigurationId}.",
                        UserName =
                            userName.Trim()
                    });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message =
                        "Sección eliminada de la configuración de inventario correctamente.",
                    Result = new
                    {
                        InventoryConfigurationId =
                            inventoryConfigurationId,

                        SectionId =
                            sectionId,

                        DeletedAssignments =
                            deletedAssignments
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar la sección {SectionId} " +
                    "de la configuración de inventario {InventoryConfigurationId}.",
                    sectionId,
                    inventoryConfigurationId);

                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message =
                        "Ocurrió un error al eliminar la sección de la configuración de inventario.",
                    Result = new { }
                });
            }
        }
    }
}