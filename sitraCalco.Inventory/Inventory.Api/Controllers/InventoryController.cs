using Inventory.Api.Extensions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Helpers;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryApplication _inventoryApplication;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            IInventoryApplication inventoryApplication,
            ILogger<InventoryController> logger)
        {
            _inventoryApplication = inventoryApplication;
            _logger = logger;
        }

        /// <summary>
        /// Guarda definitivamente la colección de productos correspondiente a un conteo.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateInventoryDto? request)
        {
            try
            {
                var validationError = InventoryRequestValidation.Validate(request);
                if (validationError is not null)
                    return Error(400, validationError);

                var userLogin = User.GetUserLogin();
                if (string.IsNullOrWhiteSpace(userLogin))
                    return Error(403, "El token no contiene un userLogin válido.");

                var role = User.GetRoleName();
                if (string.IsNullOrWhiteSpace(role))
                    return Error(403, "El token no contiene un nameRole válido.");

                var result = await _inventoryApplication.Create(request!, userLogin, role);
                return result.Status switch
                {
                    InventorySaveStatus.Success => Ok(new ResponseApi
                    {
                        IsSuccess = true,
                        Message = "Inventario guardado correctamente.",
                        Result = result.Data!
                    }),
                    InventorySaveStatus.InvalidRequest or InventorySaveStatus.InvalidExecution or InventorySaveStatus.Duplicate =>
                        Error(400, result.Message ?? "No se pudo guardar el inventario."),
                    InventorySaveStatus.Forbidden =>
                        Error(403, result.Message ?? "No se pudo guardar el inventario."),
                    InventorySaveStatus.InvalidContext =>
                        Error(404, result.Message ?? "No se pudo guardar el inventario."),
                    _ => throw new InvalidOperationException("Resultado de guardado de inventario no reconocido.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al guardar el inventario. Centro {SolutionCenterId}, configuración {InventoryConfigurationId}, sección {SectionId}, conteo {CountNumber}, ejecución {InventoryExecutionId}.",
                    request?.SolutionCenterId,
                    request?.InventoryConfigurationId,
                    request?.SectionId,
                    request?.CountNumber,
                    request?.InventoryExecutionId);

                return Error(500, "Ocurrió un error al guardar el inventario.");
            }
        }

        /// <summary>
        /// Consulta los conteos guardados de una ejecución, con filtro opcional de conteo.
        /// </summary>
        [HttpGet("{inventoryExecutionId}/counts")]
        public async Task<IActionResult> GetCounts(
            string inventoryExecutionId,
            [FromQuery] int? countNumber = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(inventoryExecutionId) ||
                    !Guid.TryParse(inventoryExecutionId.Trim(), out var executionId))
                    return Error(400, "El identificador de ejecución debe tener formato GUID.");

                if (countNumber.HasValue && countNumber.Value is < 1 or > 3)
                    return Error(400, "El número de conteo debe ser 1, 2 o 3.");

                var role = User.GetRoleName();
                if (string.IsNullOrWhiteSpace(role))
                    return Error(403, "El token no contiene un nameRole válido.");

                var result = await _inventoryApplication.GetCounts(executionId.ToString(), countNumber, role);
                if (result.Status == InventoryCountsStatus.Success)
                {
                    var hasRecords = result.Data!.Items.Count > 0;
                    return Ok(new ResponseApi
                    {
                        IsSuccess = hasRecords,
                        Message = hasRecords
                            ? "Conteos de inventario obtenidos correctamente."
                            : "No se encontraron registros para el conteo solicitado.",
                        Result = result.Data
                    });
                }

                return result.Status switch
                {
                    InventoryCountsStatus.InvalidRequest =>
                        Error(400, result.Message ?? "No se pudieron consultar los conteos del inventario."),
                    InventoryCountsStatus.Forbidden =>
                        Error(403, result.Message ?? "No se pudieron consultar los conteos del inventario."),
                    InventoryCountsStatus.NotFound =>
                        Error(404, result.Message ?? "No se pudieron consultar los conteos del inventario."),
                    _ => throw new InvalidOperationException("Resultado de consulta de conteos no reconocido.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al consultar los conteos de la ejecución {InventoryExecutionId}. Conteo {CountNumber}.",
                    inventoryExecutionId, countNumber);
                return Error(500, "Error al consultar los conteos del inventario.");
            }
        }

        private ObjectResult Error(int statusCode, string message)
        {
            return StatusCode(statusCode, new ResponseApi
            {
                IsSuccess = false,
                Message = message,
                Result = new { }
            });
        }
    }
}
