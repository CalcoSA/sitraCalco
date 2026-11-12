using FluentValidation;
using FluentValidation.Results;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Inventory.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using System.Text.Json;

namespace Inventory.Test.Controllers
{
    public class PermissionControllerTests
    {
        private readonly Mock<IPermissionApplication> _application = new();
        private readonly Mock<ILogApplication> _logs = new();
        private readonly Mock<IValidator<CreatePermissionDto>> _createValidator = new();
        private readonly Mock<IValidator<UpdatePermissionDto>> _updateValidator = new();
        private readonly PermissionController _controller;

        public PermissionControllerTests()
        {
            _createValidator.Setup(x => x.ValidateAsync(
                It.IsAny<CreatePermissionDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _updateValidator.Setup(x => x.ValidateAsync(
                It.IsAny<UpdatePermissionDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _controller = new PermissionController(
                _application.Object, _logs.Object, _createValidator.Object, _updateValidator.Object,
                NullLogger<PermissionController>.Instance).WithIdentity();
        }

        private static PermissionDto Permission() => new()
        {
            PermissionId = 1,
            PermissionKey = "VIEW_EXAMPLE",
            PermissionValue = "COSTOS"
        };

        private static CreatePermissionDto CreateRequest() => new()
        {
            PermissionKey = " view_example ",
            PermissionValue = " costos, ALMACEN ,costos "
        };

        private static UpdatePermissionDto UpdateRequest() => new()
        {
            PermissionKey = " view_example ",
            PermissionValue = " costos, ALMACEN ,costos "
        };

        private static ResponseApi AssertResponse(IActionResult result, int statusCode)
        {
            var response = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(statusCode, response.StatusCode);
            return Assert.IsType<ResponseApi>(response.Value);
        }

        private void VerifyLog(string action, string description)
        {
            _logs.Verify(x => x.CreateLog(It.Is<CreateLogDto>(log =>
                log.Action == action && log.Module == "ConfiguracionPermisos" &&
                log.Description == description && log.UserName == "juan.zapata")), Times.Once);
        }

        [Fact]
        public async Task GetAll_ShouldReturnRecords()
        {
            _application.Setup(x => x.GetAll()).ReturnsAsync(new[] { Permission() });

            var response = AssertResponse(await _controller.GetAll(), 200);

            Assert.True(response.IsSuccess);
            Assert.Single(Assert.IsAssignableFrom<IEnumerable<PermissionDto>>(response.Result));
        }

        [Fact]
        public async Task GetAll_ShouldReturnEmptyList_WhenNoRecordsExist()
        {
            _application.Setup(x => x.GetAll()).ReturnsAsync(Array.Empty<PermissionDto>());

            var response = AssertResponse(await _controller.GetAll(), 200);

            Assert.False(response.IsSuccess);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<PermissionDto>>(response.Result));
        }

        [Fact]
        public async Task GetById_ShouldReturnPermission()
        {
            _application.Setup(x => x.GetById(1)).ReturnsAsync(Permission());

            var response = AssertResponse(await _controller.GetById(1), 200);

            Assert.Equal("VIEW_EXAMPLE", Assert.IsType<PermissionDto>(response.Result).PermissionKey);
        }

        [Fact]
        public async Task GetById_ShouldReturn404_WhenMissing()
        {
            AssertResponse(await _controller.GetById(1), 404);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetById_ShouldRejectInvalidId(long id)
        {
            AssertResponse(await _controller.GetById(id), 400);
            _application.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetByKey_ShouldReturnPermission()
        {
            _application.Setup(x => x.GetByKey("view_example")).ReturnsAsync(Permission());

            var response = AssertResponse(await _controller.GetByKey("view_example"), 200);

            Assert.Equal("VIEW_EXAMPLE", Assert.IsType<PermissionDto>(response.Result).PermissionKey);
        }

        [Fact]
        public async Task GetByKey_ShouldReturn404_WhenMissing()
        {
            AssertResponse(await _controller.GetByKey("VIEW_UNKNOWN"), 404);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByKey_ShouldRejectEmptyKey(string key)
        {
            AssertResponse(await _controller.GetByKey(key), 400);
            _application.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldCreatePermission_AndAuditClaimIgnoringSuppliedUser()
        {
            _controller.WithIdentity(userLogin: "  juan.zapata  ");
            _controller.Request.Headers["X-User"] = "suplantado";
            _controller.Request.QueryString = new QueryString("?userLogin=suplantado&role=ADMINISTRADOR");
            var request = JsonSerializer.Deserialize<CreatePermissionDto>(
                """{"permissionKey":" view_example ","permissionValue":"COSTOS","userLogin":"suplantado","userName":"suplantado"}""",
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            _application.Setup(x => x.Create(request)).ReturnsAsync(10);

            var response = AssertResponse(await _controller.Create(request), 200);

            Assert.True(response.IsSuccess);
            VerifyLog("Crear", "Se creó el permiso VIEW_EXAMPLE.");
        }

        [Fact]
        public async Task Create_ShouldReturn400_WhenKeyIsDuplicated()
        {
            _application.Setup(x => x.Create(It.IsAny<CreatePermissionDto>())).ReturnsAsync(0);

            AssertResponse(await _controller.Create(CreateRequest()), 400);

            _logs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldReturnValidationErrors_WithoutCallingApplication()
        {
            _createValidator.Setup(x => x.ValidateAsync(
                It.IsAny<CreatePermissionDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("PermissionKey", "Clave obligatoria.")
                }));

            var response = AssertResponse(await _controller.Create(CreateRequest()), 400);

            Assert.Contains("Clave obligatoria.", Assert.IsAssignableFrom<IEnumerable<string>>(response.Result));
            _application.VerifyNoOtherCalls();
            _logs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldReturnNormalizedValues_AndAuditClaim()
        {
            var request = UpdateRequest();
            _application.Setup(x => x.GetById(1)).ReturnsAsync(Permission());
            _application.Setup(x => x.Update(1, request)).ReturnsAsync(true);
            _controller.Request.Headers["X-User"] = "suplantado";

            var response = AssertResponse(await _controller.Update(1, request), 200);

            var permission = Assert.IsType<PermissionDto>(response.Result);
            Assert.Equal("VIEW_EXAMPLE", permission.PermissionKey);
            Assert.Equal("COSTOS,ALMACEN", permission.PermissionValue);
            VerifyLog("Actualizar", "Se actualizó el permiso VIEW_EXAMPLE.");
        }

        [Fact]
        public async Task Update_ShouldReturn404_WhenMissing()
        {
            AssertResponse(await _controller.Update(1, UpdateRequest()), 404);
            _application.Verify(x => x.Update(It.IsAny<long>(), It.IsAny<UpdatePermissionDto>()), Times.Never);
            _logs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldReturn400_WhenKeyIsDuplicated()
        {
            _application.Setup(x => x.GetById(1)).ReturnsAsync(Permission());
            _application.Setup(x => x.Update(1, It.IsAny<UpdatePermissionDto>())).ReturnsAsync(false);

            AssertResponse(await _controller.Update(1, UpdateRequest()), 400);

            _logs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldReturn404_WhenDeletedConcurrently()
        {
            _application.SetupSequence(x => x.GetById(1))
                .ReturnsAsync(Permission()).ReturnsAsync((PermissionDto?)null);
            _application.Setup(x => x.Update(1, It.IsAny<UpdatePermissionDto>())).ReturnsAsync(false);

            AssertResponse(await _controller.Update(1, UpdateRequest()), 404);
            _logs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldReturnValidationErrors()
        {
            _updateValidator.Setup(x => x.ValidateAsync(
                It.IsAny<UpdatePermissionDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("PermissionValue", "Roles obligatorios.")
                }));

            AssertResponse(await _controller.Update(1, UpdateRequest()), 400);
            _application.VerifyNoOtherCalls();
            _logs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldDeletePermission_AndAuditClaim()
        {
            _application.Setup(x => x.GetById(1)).ReturnsAsync(Permission());
            _application.Setup(x => x.Delete(1)).ReturnsAsync(true);
            _controller.Request.Headers["X-User"] = "suplantado";

            AssertResponse(await _controller.Delete(1), 200);

            VerifyLog("Eliminar", "Se eliminó el permiso VIEW_EXAMPLE.");
        }

        [Fact]
        public async Task Delete_ShouldReturn404_WhenMissing()
        {
            AssertResponse(await _controller.Delete(1), 404);
            _application.Verify(x => x.Delete(It.IsAny<long>()), Times.Never);
            _logs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldReturn404_WhenDeletedConcurrently()
        {
            _application.Setup(x => x.GetById(1)).ReturnsAsync(Permission());
            _application.Setup(x => x.Delete(1)).ReturnsAsync(false);

            AssertResponse(await _controller.Delete(1), 404);
            _logs.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task Mutations_ShouldRejectMissingUserClaim_EvenWithAlternativeHeader(string method)
        {
            _controller.WithIdentity(userLogin: null);
            _controller.Request.Headers["X-User"] = "suplantado";

            var result = method switch
            {
                "POST" => await _controller.Create(CreateRequest()),
                "PUT" => await _controller.Update(1, UpdateRequest()),
                _ => await _controller.Delete(1)
            };

            AssertResponse(result, 403);
            _application.VerifyNoOtherCalls();
            _logs.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetCurrentRole_ShouldUseClaim_IgnoringQueryHeaderAndBody()
        {
            _controller.WithIdentity(role: "ALMACEN");
            _controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");
            _controller.Request.Headers["role"] = "ADMINISTRADOR";
            _controller.Request.ContentType = "application/json";
            _controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"role":"ADMINISTRADOR"}"""));
            var permissions = new[] { new PermissionDto
            {
                PermissionId = 2, PermissionKey = "VIEW_WAREHOUSES_ONLY", PermissionValue = "ALMACEN"
            } };
            _application.Setup(x => x.GetPermissionsByRole("ALMACEN")).ReturnsAsync(permissions);

            var response = AssertResponse(await _controller.GetCurrentRole(), 200);

            Assert.True(response.IsSuccess);
            Assert.Equal("Permisos del rol consultados correctamente.", response.Message);
            Assert.Equal(permissions, Assert.IsAssignableFrom<IEnumerable<PermissionDto>>(response.Result));
            _application.Verify(x => x.GetPermissionsByRole("ALMACEN"), Times.Once);
            _application.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetCurrentRole_ShouldReturn403_WhenClaimIsMissingOrEmpty(string? role)
        {
            _controller.WithIdentity(role: role);
            _controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");
            _controller.Request.Headers["role"] = "ADMINISTRADOR";

            AssertResponse(await _controller.GetCurrentRole(), 403);
            _application.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetCurrentRole_ShouldReturnEmptyList_WhenNoPermissionsAreAssigned()
        {
            _application.Setup(x => x.GetPermissionsByRole("COSTOS"))
                .ReturnsAsync(Array.Empty<PermissionDto>());

            var response = AssertResponse(await _controller.GetCurrentRole(), 200);

            Assert.True(response.IsSuccess);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<PermissionDto>>(response.Result));
        }

        [Theory]
        [InlineData("all")]
        [InlineData("id")]
        [InlineData("key")]
        [InlineData("current-role")]
        [InlineData("create")]
        [InlineData("update")]
        [InlineData("delete")]
        public async Task Actions_ShouldReturn500_WhenApplicationThrows(string action)
        {
            var exception = new InvalidOperationException("Fallo simulado.");
            _application.Setup(x => x.GetAll()).ThrowsAsync(exception);
            _application.Setup(x => x.GetById(1)).ThrowsAsync(exception);
            _application.Setup(x => x.GetByKey("VIEW_EXAMPLE")).ThrowsAsync(exception);
            _application.Setup(x => x.GetPermissionsByRole("COSTOS")).ThrowsAsync(exception);
            _application.Setup(x => x.Create(It.IsAny<CreatePermissionDto>())).ThrowsAsync(exception);

            var result = action switch
            {
                "all" => await _controller.GetAll(),
                "id" => await _controller.GetById(1),
                "key" => await _controller.GetByKey("VIEW_EXAMPLE"),
                "current-role" => await _controller.GetCurrentRole(),
                "create" => await _controller.Create(CreateRequest()),
                "update" => await _controller.Update(1, UpdateRequest()),
                _ => await _controller.Delete(1)
            };

            var response = AssertResponse(result, 500);
            Assert.False(response.IsSuccess);
            Assert.DoesNotContain("Fallo simulado", response.Message);
            _logs.VerifyNoOtherCalls();
        }
    }
}
