using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Inventory.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Inventory.Test.Controllers
{
    public class InventoryControllerTests
    {
        private readonly Mock<IInventoryApplication> _applicationMock = new();
        private readonly Mock<ILogger<InventoryController>> _loggerMock = new();
        private readonly InventoryController _controller;

        public InventoryControllerTests()
        {
            _controller = new InventoryController(
                _applicationMock.Object,
                _loggerMock.Object).WithIdentity();
        }

        private static CreateInventoryDto CreateRequest() => new()
        {
            SolutionCenterId = 2,
            InventoryConfigurationId = 1,
            SectionId = 2,
            CountNumber = 1,
            EnteredBy = " empleado.inventario ",
            Items = new List<CreateInventoryItemDto?>
            {
                new CreateInventoryItemDto
                {
                    ProductId = 10,
                    Open = 3,
                    Closed = 2,
                    MultiplicationValue = 15
                }
            }
        };

        private static ResponseApi AssertError(IActionResult result, int statusCode, string message)
        {
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(statusCode, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal(message, response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
            return response;
        }

        [Fact]
        public async Task Create_ShouldReturn200AndExecutionId_UsingClaimsInsteadOfEnteredByOrSuppliedIdentity()
        {
            _controller.WithIdentity(userLogin: "  juan.zapata  ", role: "  ALMACEN  ");
            _controller.Request.Headers["X-User"] = "suplantado";
            _controller.Request.Headers["role"] = "ADMINISTRADOR";
            _controller.Request.QueryString = new QueryString("?userLogin=suplantado&role=ADMINISTRADOR&nameRole=ADMINISTRADOR");
            var request = JsonSerializer.Deserialize<CreateInventoryDto>(
                """
                {
                  "solutionCenterId": 2,
                  "inventoryConfigurationId": 1,
                  "sectionId": 2,
                  "countNumber": 1,
                  "enteredBy": " empleado.inventario ",
                  "userLogin": "suplantado",
                  "role": "ADMINISTRADOR",
                  "nameRole": "ADMINISTRADOR",
                  "items": [{ "productId": 10, "open": 3, "closed": 2, "multiplicationValue": 15 }]
                }
                """,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            var saved = new InventorySaveDto
            {
                InventoryExecutionId = "550e8400-e29b-41d4-a716-446655440000",
                CountNumber = 1,
                SavedItems = 1
            };
            _applicationMock.Setup(x => x.Create(request, "juan.zapata", "ALMACEN"))
                .ReturnsAsync(new CreateInventoryResultDto
                {
                    Status = InventorySaveStatus.Success,
                    Data = saved
                });

            var response = Assert.IsType<ResponseApi>(
                Assert.IsType<OkObjectResult>(await _controller.Create(request)).Value);

            Assert.True(response.IsSuccess);
            Assert.Equal("Inventario guardado correctamente.", response.Message);
            var result = Assert.IsType<InventorySaveDto>(response.Result);
            Assert.Same(saved, result);
            Assert.Equal("550e8400-e29b-41d4-a716-446655440000", result.InventoryExecutionId);
            Assert.Equal(1, result.CountNumber);
            Assert.Equal(1, result.SavedItems);
            Assert.Equal(" empleado.inventario ", request.EnteredBy);
            _applicationMock.Verify(x => x.Create(request, "juan.zapata", "ALMACEN"), Times.Once);
            _applicationMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldReturn400_WhenRequestIsMissing()
        {
            AssertError(await _controller.Create(null), 400, "Debe enviar los datos del inventario.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(0L, 1L, 2L, "El identificador de la bodega o punto de venta debe ser mayor a cero.")]
        [InlineData(-1L, 1L, 2L, "El identificador de la bodega o punto de venta debe ser mayor a cero.")]
        [InlineData(2L, 0L, 2L, "El identificador de la configuración de inventario debe ser mayor a cero.")]
        [InlineData(2L, -1L, 2L, "El identificador de la configuración de inventario debe ser mayor a cero.")]
        [InlineData(2L, 1L, 0L, "El identificador de la sección debe ser mayor a cero.")]
        [InlineData(2L, 1L, -1L, "El identificador de la sección debe ser mayor a cero.")]
        public async Task Create_ShouldReturn400_WhenContextIdIsInvalid(
            long solutionCenterId, long configurationId, long sectionId, string message)
        {
            var request = CreateRequest();
            request.SolutionCenterId = solutionCenterId;
            request.InventoryConfigurationId = configurationId;
            request.SectionId = sectionId;

            AssertError(await _controller.Create(request), 400, message);
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public async Task Create_ShouldReturn400_WhenCountNumberIsInvalid(int countNumber)
        {
            var request = CreateRequest();
            request.CountNumber = countNumber;

            AssertError(await _controller.Create(request), 400, "El número de conteo debe ser 1, 2 o 3.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_ShouldReturn400_WhenEnteredByIsMissing(string? enteredBy)
        {
            var request = CreateRequest();
            request.EnteredBy = enteredBy;

            AssertError(await _controller.Create(request), 400,
                "El responsable enteredBy es obligatorio y no puede superar 150 caracteres.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Create_ShouldReturn400_WhenItemsAreMissing(bool isNull)
        {
            var request = CreateRequest();
            request.Items = isNull ? null : new List<CreateInventoryItemDto?>();

            AssertError(await _controller.Create(request), 400, "Debe enviar al menos un producto en items.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0L)]
        [InlineData(-1L)]
        public async Task Create_ShouldReturn400_WhenItemIsNullOrProductIdIsInvalid(long? productId)
        {
            var request = CreateRequest();
            request.Items = new List<CreateInventoryItemDto?>
            {
                productId.HasValue ? new CreateInventoryItemDto { ProductId = productId.Value } : null
            };

            AssertError(await _controller.Create(request), 400,
                "Todos los items deben contener un productId mayor a cero.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_ShouldReturn403_WhenUserLoginClaimIsMissing(string? userLogin)
        {
            _controller.WithIdentity(userLogin: userLogin);
            _controller.Request.Headers["X-User"] = "suplantado";
            _controller.Request.QueryString = new QueryString("?userLogin=suplantado");

            AssertError(await _controller.Create(CreateRequest()), 403,
                "El token no contiene un userLogin válido.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldReturn403_WhenRoleClaimIsMissing()
        {
            _controller.WithIdentity(role: null);
            _controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR&nameRole=ADMINISTRADOR");

            AssertError(await _controller.Create(CreateRequest()), 403,
                "El token no contiene un nameRole válido.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(InventorySaveStatus.InvalidRequest, 400, "Uno o más productos no pertenecen al centro y sección.")]
        [InlineData(InventorySaveStatus.Forbidden, 403, "El rol no tiene permisos para guardar este inventario.")]
        [InlineData(InventorySaveStatus.InvalidContext, 404, "La sección no está disponible para esta configuración.")]
        [InlineData(InventorySaveStatus.InvalidExecution, 400, "La ejecución de inventario pertenece a otro contexto.")]
        [InlineData(InventorySaveStatus.Duplicate, 400, "El conteo ya contiene uno de los productos enviados.")]
        public async Task Create_ShouldMapApplicationFailure(
            InventorySaveStatus status, int statusCode, string message)
        {
            var request = CreateRequest();
            _applicationMock.Setup(x => x.Create(request, "juan.zapata", "COSTOS"))
                .ReturnsAsync(new CreateInventoryResultDto { Status = status, Message = message });

            AssertError(await _controller.Create(request), statusCode, message);

            _applicationMock.Verify(x => x.Create(request, "juan.zapata", "COSTOS"), Times.Once);
            _applicationMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldReturn500_WhenApplicationThrows()
        {
            var request = CreateRequest();
            _applicationMock.Setup(x => x.Create(request, "juan.zapata", "COSTOS"))
                .ThrowsAsync(new InvalidOperationException("Detalle interno de base de datos."));

            AssertError(await _controller.Create(request), 500,
                "Ocurrió un error al guardar el inventario.");

            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        private const string ExecutionId = "97c3187f-045d-4acd-9427-a7f5841911eb";

        private static InventoryCountsDto CountsData(string countMode = "OPEN_CLOSED") => new()
        {
            InventoryExecutionId = ExecutionId,
            SolutionCenterId = 2,
            SolutionCenterTypeId = countMode == "SINGLE" ? 1 : 2,
            SolutionCenterCode = "002",
            SolutionCenterName = "Poblado",
            InventoryConfigurationId = 1,
            InventoryConfigurationName = "Uno a uno",
            SectionId = 2,
            SectionName = "Línea de sal",
            CountMode = countMode
        };

        private void SetupCounts(InventoryCountsDto data, int? countNumber, string role)
        {
            _applicationMock.Setup(x => x.GetCounts(ExecutionId, countNumber, role))
                .ReturnsAsync(new InventoryCountsResultDto
                {
                    Status = InventoryCountsStatus.Success,
                    Data = data
                });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ejecucion-invalida")]
        public async Task GetCounts_ShouldReturn400_WhenExecutionIdIsMissingOrInvalid(string? executionId)
        {
            AssertError(await _controller.GetCounts(executionId!), 400,
                "El identificador de ejecución debe tener formato GUID.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public async Task GetCounts_ShouldReturn400_WhenCountNumberIsInvalid(int countNumber)
        {
            _controller.WithIdentity(role: "LIMITED_ROLE");

            AssertError(await _controller.GetCounts(ExecutionId, countNumber), 400,
                "El número de conteo debe ser 1, 2 o 3.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetCounts_ShouldReturn403_WhenRoleClaimIsMissing(string? role)
        {
            _controller.WithIdentity(role: role);
            _controller.Request.Headers["role"] = "FULL_ROLE";
            _controller.Request.QueryString = new QueryString("?role=FULL_ROLE&nameRole=FULL_ROLE");

            AssertError(await _controller.GetCounts(ExecutionId), 403,
                "El token no contiene un nameRole válido.");
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(InventoryCountsStatus.NotFound, 404, "La ejecución de inventario no existe.")]
        [InlineData(InventoryCountsStatus.Forbidden, 403, "El rol no tiene permisos para consultar el detalle del inventario.")]
        [InlineData(InventoryCountsStatus.Forbidden, 403, "El rol no tiene permisos para consultar este centro.")]
        [InlineData(InventoryCountsStatus.InvalidRequest, 400, "Debe indicar countNumber para consultar los conteos con permiso limitado.")]
        public async Task GetCounts_ShouldMapApplicationFailure(
            InventoryCountsStatus status, int httpStatus, string message)
        {
            _applicationMock.Setup(x => x.GetCounts(ExecutionId, null, "COSTOS"))
                .ReturnsAsync(new InventoryCountsResultDto { Status = status, Message = message });

            AssertError(await _controller.GetCounts(ExecutionId), httpStatus, message);

            _applicationMock.Verify(x => x.GetCounts(ExecutionId, null, "COSTOS"), Times.Once);
            _applicationMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetCounts_ShouldReturnAllProvidedCounts_UsingOnlyRoleClaimAndNormalizedGuid()
        {
            _controller.WithIdentity(userLogin: null, role: "  FULL_ROLE  ");
            _controller.Request.Headers["role"] = "suplantado";
            _controller.Request.QueryString = new QueryString("?role=suplantado&nameRole=suplantado");
            var data = CountsData();
            data.Items.Add(new InventoryProductCountsDto
            {
                ProductId = 10,
                Counts = new List<InventoryCountDto>
                {
                    new InventoryCountDto { CountNumber = 1, Open = 3, Closed = 2, MultiplicationValue = 15 },
                    new InventoryCountDto { CountNumber = 2, Open = 4, Closed = 2, MultiplicationValue = 18 },
                    new InventoryCountDto { CountNumber = 3, Open = 4, Closed = 3, MultiplicationValue = 21 }
                }
            });
            SetupCounts(data, null, "FULL_ROLE");

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(
                await _controller.GetCounts("  " + ExecutionId.ToUpperInvariant() + "  ")).Value);

            Assert.True(response.IsSuccess);
            Assert.Equal("Conteos de inventario obtenidos correctamente.", response.Message);
            var result = Assert.IsType<InventoryCountsDto>(response.Result);
            Assert.Same(data, result);
            Assert.Equal(ExecutionId, result.InventoryExecutionId);
            Assert.Equal("OPEN_CLOSED", result.CountMode);
            var product = Assert.Single(result.Items);
            Assert.Equal(10, product.ProductId);
            Assert.Equal(new[] { 1, 2, 3 }, product.Counts.Select(count => count.CountNumber));
            Assert.All(product.Counts, count => Assert.Null(count.Value));
            Assert.Equal(3m, product.Counts[0].Open);
            Assert.Equal(2m, product.Counts[0].Closed);
            Assert.Equal(new decimal?[] { 15, 18, 21 }, product.Counts.Select(count => count.MultiplicationValue));
            _applicationMock.Verify(x => x.GetCounts(ExecutionId, null, "FULL_ROLE"), Times.Once);
            _applicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("FULL_ROLE")]
        [InlineData("LIMITED_ROLE")]
        public async Task GetCounts_ShouldPreserveFilteredApplicationResponse(string role)
        {
            _controller.WithIdentity(role: role);
            var data = CountsData();
            data.Items.Add(new InventoryProductCountsDto
            {
                ProductId = 25,
                Counts = new List<InventoryCountDto>
                {
                    new InventoryCountDto { CountNumber = 2, Open = 3, Closed = 1, MultiplicationValue = 12 }
                }
            });
            SetupCounts(data, 2, role);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(
                await _controller.GetCounts(ExecutionId, 2)).Value);

            Assert.True(response.IsSuccess);
            Assert.Same(data, response.Result);
            var count = Assert.Single(Assert.Single(data.Items).Counts);
            Assert.Equal(2, count.CountNumber);
            Assert.Equal(12m, count.MultiplicationValue);
            _applicationMock.Verify(x => x.GetCounts(ExecutionId, 2, role), Times.Once);
            _applicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetCounts_ShouldReturn200WithEmptyItems_WhenRequestedCountHasNoRecords()
        {
            var data = CountsData();
            SetupCounts(data, 3, "COSTOS");

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(
                await _controller.GetCounts(ExecutionId, 3)).Value);

            Assert.False(response.IsSuccess);
            Assert.Equal("No se encontraron registros para el conteo solicitado.", response.Message);
            Assert.Same(data, response.Result);
            Assert.Equal(ExecutionId, data.InventoryExecutionId);
            Assert.Empty(data.Items);
            _applicationMock.Verify(x => x.GetCounts(ExecutionId, 3, "COSTOS"), Times.Once);
        }

        [Fact]
        public async Task GetCounts_ShouldPreserveSingleModeAndMultiplicationValue()
        {
            var data = CountsData("SINGLE");
            data.Items.Add(new InventoryProductCountsDto
            {
                ProductId = 10,
                Counts = new List<InventoryCountDto>
                {
                    new InventoryCountDto { CountNumber = 1, Value = 8, MultiplicationValue = 24 }
                }
            });
            SetupCounts(data, 1, "COSTOS");

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(
                await _controller.GetCounts(ExecutionId, 1)).Value);

            Assert.True(response.IsSuccess);
            var result = Assert.IsType<InventoryCountsDto>(response.Result);
            Assert.Same(data, result);
            Assert.Equal("SINGLE", result.CountMode);
            var count = Assert.Single(Assert.Single(result.Items).Counts);
            Assert.Equal(8m, count.Value);
            Assert.Null(count.Open);
            Assert.Null(count.Closed);
            Assert.Equal(24m, count.MultiplicationValue);
        }

        [Fact]
        public async Task GetCounts_ShouldReturn500_WhenApplicationThrows()
        {
            _applicationMock.Setup(x => x.GetCounts(ExecutionId, null, "COSTOS"))
                .ThrowsAsync(new InvalidOperationException("Detalle interno de base de datos."));

            AssertError(await _controller.GetCounts(ExecutionId), 500,
                "Error al consultar los conteos del inventario.");

            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }
}
