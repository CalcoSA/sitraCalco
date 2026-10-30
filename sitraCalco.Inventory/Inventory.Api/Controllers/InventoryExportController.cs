using System.Globalization;
using Inventory.Api.Extensions;
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
    public class InventoryExportController : ControllerBase
    {
        private readonly IInventoryExportApplication _application;
        private readonly ILogger<InventoryExportController> _logger;
        public InventoryExportController(IInventoryExportApplication application, ILogger<InventoryExportController> logger)
        {
            _application = application;
            _logger = logger;
        }

        /// <summary>Consulta los datos históricos y parámetros para construir planos SIESA.</summary>
        /// <param name="date">Fecha obligatoria YYYY-MM-DD, según created_at almacenado en SITRA.</param>
        /// <param name="solutionCenterIds">IDs positivos opcionales; repetir el parámetro para seleccionar varios centros.</param>
        /// <param name="cancellationToken">Cancelación de la solicitud.</param>
        [HttpGet("siesa")]
        public async Task<IActionResult> GetSiesa([FromQuery] string? date, [FromQuery] string[]? solutionCenterIds = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Parseo local para que los errores de IDs conserven ResponseApi.
                var ids = new List<long>();
                foreach (var value in solutionCenterIds ?? Array.Empty<string>())
                {
                    if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                        return Error(400, "solutionCenterIds debe contener identificadores enteros mayores a cero.");
                    ids.Add(id);
                }
                var result = await _application.GetSiesa(date, ids, User.GetRoleName(), cancellationToken);
                var statusCode = result.Status switch
                {
                    InventoryExportStatus.Success => 200,
                    InventoryExportStatus.InvalidRequest => 400,
                    InventoryExportStatus.Forbidden => 403,
                    InventoryExportStatus.NotFound => 404,
                    InventoryExportStatus.InvalidData => 409,
                    InventoryExportStatus.InvalidParameters => 500,
                    _ => throw new InvalidOperationException("Resultado de exportación no reconocido.")
                };
                return StatusCode(statusCode, new ResponseApi
                {
                    IsSuccess = result.Status == InventoryExportStatus.Success,
                    Message = result.Message!,
                    Result = (object?)result.Data ?? new { }
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar la exportación SIESA para la fecha {Date}.", date);
                return Error(500, "Ocurrió un error al consultar la información para planos SIESA.");
            }
        }

        private ObjectResult Error(int code, string message) => StatusCode(code,
            new ResponseApi { IsSuccess = false, Message = message, Result = new { } });
    }
}
