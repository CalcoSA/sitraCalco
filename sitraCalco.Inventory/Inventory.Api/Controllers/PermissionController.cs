using FluentValidation;
using Inventory.Api.Extensions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Helpers;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionApplication _permissionApplication;
        private readonly ILogApplication _logApplication;
        private readonly IValidator<CreatePermissionDto> _createPermissionValidator;
        private readonly IValidator<UpdatePermissionDto> _updatePermissionValidator;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(
            IPermissionApplication permissionApplication,
            ILogApplication logApplication,
            IValidator<CreatePermissionDto> createPermissionValidator,
            IValidator<UpdatePermissionDto> updatePermissionValidator,
            ILogger<PermissionController> logger)
        {
            _permissionApplication = permissionApplication;
            _logApplication = logApplication;
            _createPermissionValidator = createPermissionValidator;
            _updatePermissionValidator = updatePermissionValidator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var permissions = (await _permissionApplication.GetAll()).ToList();

                return Ok(new ResponseApi
                {
                    IsSuccess = permissions.Any(),
                    Message = permissions.Any()
                        ? "Permisos consultados correctamente."
                        : "No hay permisos registrados.",
                    Result = permissions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los permisos.");
                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al consultar los permisos.",
                    Result = new { }
                });
            }
        }

        [HttpGet("{permissionId:long}")]
        public async Task<IActionResult> GetById(long permissionId)
        {
            try
            {
                if (permissionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El identificador del permiso debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                var permission = await _permissionApplication.GetById(permissionId);
                if (permission is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El permiso no existe.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Permiso consultado correctamente.",
                    Result = permission
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el permiso {PermissionId}.", permissionId);
                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al consultar el permiso.",
                    Result = new { }
                });
            }
        }

        [HttpGet("key/{permissionKey}")]
        public async Task<IActionResult> GetByKey(string permissionKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(permissionKey) || permissionKey.Length > 150)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La clave del permiso es obligatoria y no puede superar los 150 caracteres.",
                        Result = new { }
                    });
                }

                var permission = await _permissionApplication.GetByKey(permissionKey);
                if (permission is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El permiso no existe.",
                        Result = new { }
                    });
                }

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Permiso consultado correctamente.",
                    Result = permission
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el permiso {PermissionKey}.", permissionKey);
                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al consultar el permiso.",
                    Result = new { }
                });
            }
        }

        [HttpGet("current-role")]
        public async Task<IActionResult> GetCurrentRole()
        {
            var role = User.GetRoleName();

            try
            {
                if (string.IsNullOrWhiteSpace(role))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El token no contiene un nameRole válido.",
                        Result = new { }
                    });
                }

                var permissions = (await _permissionApplication.GetPermissionsByRole(role)).ToList();
                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Permisos del rol consultados correctamente.",
                    Result = permissions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los permisos del rol {Role}.", role);
                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al consultar los permisos del rol.",
                    Result = new { }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePermissionDto request)
        {
            var userName = User.GetUserLogin();

            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El token no contiene un userLogin válido.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La solicitud no es válida.",
                        Result = new { }
                    });
                }

                var validation = await _createPermissionValidator.ValidateAsync(request);
                if (!validation.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La solicitud no es válida.",
                        Result = validation.Errors.Select(error => error.ErrorMessage)
                    });
                }

                var permissionId = await _permissionApplication.Create(request);
                if (permissionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se pudo crear el permiso. Verifique que la clave no esté registrada.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(new CreateLogDto
                {
                    Action = "Crear",
                    Module = "ConfiguracionPermisos",
                    Description = $"Se creó el permiso {PermissionValues.NormalizeKey(request.PermissionKey)}.",
                    UserName = userName
                });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Permiso creado correctamente.",
                    Result = new { PermissionId = permissionId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el permiso.");
                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al crear el permiso.",
                    Result = new { }
                });
            }
        }

        [HttpPut("{permissionId:long}")]
        public async Task<IActionResult> Update(long permissionId, [FromBody] UpdatePermissionDto request)
        {
            var userName = User.GetUserLogin();

            try
            {
                if (permissionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El identificador del permiso debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El token no contiene un userLogin válido.",
                        Result = new { }
                    });
                }

                if (request is null)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La solicitud no es válida.",
                        Result = new { }
                    });
                }

                var validation = await _updatePermissionValidator.ValidateAsync(request);
                if (!validation.IsValid)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "La solicitud no es válida.",
                        Result = validation.Errors.Select(error => error.ErrorMessage)
                    });
                }

                var currentPermission = await _permissionApplication.GetById(permissionId);
                if (currentPermission is null)
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El permiso no existe.",
                        Result = new { }
                    });
                }

                if (!await _permissionApplication.Update(permissionId, request))
                {
                    // Puede haber sido eliminado entre la lectura y la actualización.
                    if (await _permissionApplication.GetById(permissionId) is null)
                    {
                        return NotFound(new ResponseApi
                        {
                            IsSuccess = false,
                            Message = "El permiso no existe.",
                            Result = new { }
                        });
                    }

                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "No se pudo actualizar el permiso. Verifique que la clave no esté registrada.",
                        Result = new { }
                    });
                }

                var permissionKey = PermissionValues.NormalizeKey(request.PermissionKey);
                await _logApplication.CreateLog(new CreateLogDto
                {
                    Action = "Actualizar",
                    Module = "ConfiguracionPermisos",
                    Description = $"Se actualizó el permiso {permissionKey}.",
                    UserName = userName
                });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Permiso actualizado correctamente.",
                    Result = new PermissionDto
                    {
                        PermissionId = permissionId,
                        PermissionKey = permissionKey,
                        PermissionValue = PermissionValues.NormalizeRoles(request.PermissionValue)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el permiso {PermissionId}.", permissionId);
                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al actualizar el permiso.",
                    Result = new { }
                });
            }
        }

        [HttpDelete("{permissionId:long}")]
        public async Task<IActionResult> Delete(long permissionId)
        {
            var userName = User.GetUserLogin();

            try
            {
                if (permissionId <= 0)
                {
                    return BadRequest(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El identificador del permiso debe ser mayor a cero.",
                        Result = new { }
                    });
                }

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El token no contiene un userLogin válido.",
                        Result = new { }
                    });
                }

                var currentPermission = await _permissionApplication.GetById(permissionId);
                if (currentPermission is null || !await _permissionApplication.Delete(permissionId))
                {
                    return NotFound(new ResponseApi
                    {
                        IsSuccess = false,
                        Message = "El permiso no existe.",
                        Result = new { }
                    });
                }

                await _logApplication.CreateLog(new CreateLogDto
                {
                    Action = "Eliminar",
                    Module = "ConfiguracionPermisos",
                    Description = $"Se eliminó el permiso {currentPermission.PermissionKey}.",
                    UserName = userName
                });

                return Ok(new ResponseApi
                {
                    IsSuccess = true,
                    Message = "Permiso eliminado correctamente.",
                    Result = new { PermissionId = permissionId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el permiso {PermissionId}.", permissionId);
                return StatusCode(500, new ResponseApi
                {
                    IsSuccess = false,
                    Message = "Ocurrió un error al eliminar el permiso.",
                    Result = new { }
                });
            }
        }
    }
}
