using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Responses;
using Microsoft.AspNetCore.Authorization;
using KitchenTraceability.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace KitchenTraceability.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReceptionLotController : ControllerBase
    {
        private readonly IValidator<CreateReceptionLotEvaluationDto> _createValidator;
        private readonly IValidator<CreateReceptionLotDto> _createLotValidator;
        private readonly ILogger<ReceptionLotController> _logger;
        private readonly IReceptionLotApplication _application;

        public ReceptionLotController(IValidator<CreateReceptionLotEvaluationDto> createValidator,
            IValidator<CreateReceptionLotDto> createLotValidator,
            ILogger<ReceptionLotController> logger,
            IReceptionLotApplication application)
        {
            _createLotValidator = createLotValidator;
            _createValidator = createValidator;
            _application = application;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReceptionLotDto request)
        {
            try
            {
                var validationResult = await _createLotValidator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return BadRequest(
                        new ResponseApi
                        {
                            IsSuccess = false,
                            Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                            Result = false
                        });
                }

                var userLogin = User.FindFirst("userLogin")?.Value;

                if (string.IsNullOrWhiteSpace(userLogin))
                {
                    return Unauthorized(
                        new ResponseApi
                        {
                            IsSuccess = false,
                            Message = "No fue posible identificar el usuario autenticado.",
                            Result = false
                        });
                }

                var response = await _application.CreateLot(request, userLogin);

                string message;

                if (response.lot_status == "REJECTED")
                {
                    message = "Lote rechazado y registro de no conformidad creado correctamente.";
                }
                else if (response.lot_status == "PENDING_APPROVAL")
                {
                    message = "Lote registrado pendiente de aprobación.";
                }
                else
                {
                    message = "Lote registrado correctamente.";
                }

                return Ok(
                    new ResponseApi
                    {
                        IsSuccess = true,
                        Message = message,
                        Result = response
                    });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Result = false
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Result = false
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar el lote de recepción.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al registrar el lote.",
                        Result = false
                    });
            }
        }

        [HttpPost("evaluation")]
        public async Task<IActionResult> CreateEvaluation([FromBody] CreateReceptionLotEvaluationDto request)
        {
            try
            {
                var validationResult = await _createValidator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return BadRequest(
                        new ResponseApi
                        {
                            IsSuccess = false,
                            Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                            Result = false
                        });
                }

                var userLogin = User.FindFirst("userLogin")?.Value;

                if (string.IsNullOrWhiteSpace(userLogin))
                {
                    return Unauthorized(
                        new ResponseApi
                        {
                            IsSuccess = false,
                            Message = "No fue posible identificar el usuario autenticado.",
                            Result = false
                        });
                }

                var response = await _application.Create(request, userLogin);

                string message;

                if (response.lot_status == "REJECTED")
                {
                    message = "Evaluación registrada. El lote fue rechazado y se generó una no conformidad.";
                }
                else if (response.nonconformity_reason == "VISUAL_INSPECTION")
                {
                    message = "Evaluación registrada y no conformidad de inspección visual generada correctamente.";
                }
                else if (response.lot_status == "PENDING_APPROVAL")
                {
                    message = "Evaluación registrada. El lote queda pendiente de aprobación.";
                }
                else
                {
                    message = "Evaluación del lote registrada correctamente.";
                }

                return Ok(
                    new ResponseApi
                    {
                        IsSuccess = true,
                        Message = message,
                        Result = response
                    });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Result = false
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Result = false
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar la evaluación del lote.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al registrar la evaluación del lote.",
                        Result = false
                    });
            }
        }
    }
}