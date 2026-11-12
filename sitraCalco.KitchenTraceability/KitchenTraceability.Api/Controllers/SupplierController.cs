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
    public class SupplierController : ControllerBase
    {
        private readonly IValidator<CreateSupplierDto> _createSupplierValidator;
        private readonly IValidator<UpdateSupplierDto> _updateSupplierValidator;
        private readonly IValidator<PaginationDto> _paginationValidator;
        private readonly ISupplierApplication _supplierApplication;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(IValidator<CreateSupplierDto> createSupplierValidator,
            IValidator<UpdateSupplierDto> updateSupplierValidator,
            IValidator<PaginationDto> paginationValidator,
            ISupplierApplication supplierApplication,
            ILogger<SupplierController> logger)
        {
            _createSupplierValidator = createSupplierValidator;
            _updateSupplierValidator = updateSupplierValidator;
            _paginationValidator = paginationValidator;
            _supplierApplication = supplierApplication;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination)
        {
            try
            {
                var validationResult = await _paginationValidator.ValidateAsync(pagination);

                if (!validationResult.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                        Result = new { }
                    });
                }

                var response = await _supplierApplication.GetAll(pagination.Page, pagination.Take);

                if (response.TotalRecords == 0)
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron proveedores registrados.",
                        Result = response
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Proveedores consultados correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los proveedores.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar los proveedores.",
                        Result = new { }
                    });
            }
        }

        [HttpGet("options")]
        public async Task<IActionResult> GetOptions([FromQuery] string? search = null)
        {
            try
            {
                var response = await _supplierApplication.GetOptions(search);

                if (!response.Any())
                {
                    return Ok(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se encontraron proveedores.",
                        Result = response
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Proveedores consultados correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los proveedores para la lista desplegable.");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Ocurrió un error al consultar los proveedores.",
                        Result = new { }
                    });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var response = await _supplierApplication.GetById(id);

                if (response is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Proveedor no encontrado.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Proveedor consultado correctamente.",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el proveedor con Id {SupplierId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al consultar el proveedor.",
                    Result = new { }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto request)
        {
            try
            {
                var validationResult = await _createSupplierValidator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                        Result = new { }
                    });
                }

                var response = await _supplierApplication.Create(request);

                if (!response)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No fue posible crear el proveedor.",
                        Result = false
                    });
                }

                return StatusCode(StatusCodes.Status201Created, new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Proveedor creado correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el proveedor.");

                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseApi
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Result = false
                });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateSupplierDto request)
        {
            try
            {
                var validationResult = await _updateSupplierValidator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", validationResult.Errors.Select(x => x.ErrorMessage)),
                        Result = new { }
                    });
                }

                var response = await _supplierApplication.Update(id, request);

                if (!response)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Proveedor no encontrado.",
                        Result = false
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Proveedor actualizado correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el proveedor con Id {SupplierId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseApi
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Result = false
                });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _supplierApplication.Delete(id);

                if (!response)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "Proveedor no encontrado.",
                        Result = false
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Proveedor eliminado correctamente.",
                    Result = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el proveedor con Id {SupplierId}.", id);

                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al eliminar el proveedor.",
                    Result = false
                });
            }
        }
    }
}