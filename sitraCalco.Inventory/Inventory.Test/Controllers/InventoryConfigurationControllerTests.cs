using Inventory.Test.Helpers;
using FluentValidation;
using FluentValidation.Results;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Inventory.Test.Controllers
{
    public class InventoryConfigurationControllerTests
    {
        private readonly Mock<IInventoryConfigurationApplication>
            _inventoryConfigurationApplicationMock;

        private readonly Mock<ILogApplication>
            _logApplicationMock;

        private readonly Mock<IValidator<CreateInventoryConfigurationDto>>
            _createInventoryConfigurationValidatorMock;

        private readonly Mock<IValidator<CreateInventoryConfigurationAssignmentsDto>>
            _createAssignmentsValidatorMock;

        private readonly Mock<IValidator<UpdateInventoryConfigurationAssignmentStatusDto>>
            _updateAssignmentStatusValidatorMock;

        private readonly Mock<IValidator<UpdateInventoryConfigurationDto>>
            _updateInventoryConfigurationValidatorMock;

        private readonly Mock<IValidator<AddInventoryConfigurationDaysDto>>
            _addInventoryConfigurationDaysValidatorMock;

        private readonly Mock<ILogger<InventoryConfigurationController>>
            _loggerMock;

        private readonly InventoryConfigurationController
            _controller;

        public InventoryConfigurationControllerTests()
        {
            _inventoryConfigurationApplicationMock =
                new Mock<IInventoryConfigurationApplication>();

            _logApplicationMock =
                new Mock<ILogApplication>();

            _createInventoryConfigurationValidatorMock =
                new Mock<IValidator<CreateInventoryConfigurationDto>>();

            _createAssignmentsValidatorMock =
                new Mock<IValidator<CreateInventoryConfigurationAssignmentsDto>>();

            _updateAssignmentStatusValidatorMock =
                new Mock<IValidator<UpdateInventoryConfigurationAssignmentStatusDto>>();

            _updateInventoryConfigurationValidatorMock =
                new Mock<IValidator<UpdateInventoryConfigurationDto>>();

            _addInventoryConfigurationDaysValidatorMock =
                new Mock<IValidator<AddInventoryConfigurationDaysDto>>();

            _loggerMock =
                new Mock<ILogger<InventoryConfigurationController>>();

            ConfigureValidValidators();

            _controller =
                new InventoryConfigurationController(
                    _inventoryConfigurationApplicationMock.Object,
                    _logApplicationMock.Object,
                    _createInventoryConfigurationValidatorMock.Object,
                    _createAssignmentsValidatorMock.Object,
                    _updateAssignmentStatusValidatorMock.Object,
                    _updateInventoryConfigurationValidatorMock.Object,
                    _addInventoryConfigurationDaysValidatorMock.Object,
                    _loggerMock.Object);
        }

        private void ConfigureValidValidators()
        {
            _createInventoryConfigurationValidatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<CreateInventoryConfigurationDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _createAssignmentsValidatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<CreateInventoryConfigurationAssignmentsDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _updateAssignmentStatusValidatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<UpdateInventoryConfigurationAssignmentStatusDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _updateInventoryConfigurationValidatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<UpdateInventoryConfigurationDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _addInventoryConfigurationDaysValidatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<AddInventoryConfigurationDaysDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        private static ValidationResult CreateInvalidValidationResult(
            string propertyName,
            string message)
        {
            return new ValidationResult(
                new[]
                {
                    new ValidationFailure(
                        propertyName,
                        message)
                });
        }

        // =========================================================
        // CREATE INVENTORY CONFIGURATION
        // =========================================================

        [Fact]
        public async Task Create_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new CreateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .Create(
                    request);

            var forbidden =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);

            var response =
                Assert.IsType<ResponseApi>(forbidden.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "El token no contiene un userLogin válido.",
                response.Message);

            _inventoryConfigurationApplicationMock.Verify(
                x => x.Create(It.IsAny<CreateInventoryConfigurationDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
        {
            var request =
                new CreateInventoryConfigurationDto
                {
                    InventoryConfigurationName = ""
                };

            _createInventoryConfigurationValidatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<CreateInventoryConfigurationDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateInvalidValidationResult(
                        nameof(CreateInventoryConfigurationDto.InventoryConfigurationName),
                        "Nombre obligatorio."));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .Create(
                    request);

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(badRequest.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal("La solicitud no es válida.", response.Message);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenApplicationReturnsZero()
        {
            var request =
                new CreateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            _inventoryConfigurationApplicationMock
                .Setup(x => x.Create(request))
                .ReturnsAsync(0);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .Create(
                    request);

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(badRequest.Value);

            Assert.False(response.IsSuccess);
        }

        [Fact]
        public async Task Create_ShouldReturnOk_WhenConfigurationIsCreated()
        {
            var request =
                new CreateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno",
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 30)
                };

            _inventoryConfigurationApplicationMock
                .Setup(x => x.Create(request))
                .ReturnsAsync(1);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .Create(
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Configuración de inventario creada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Crear" &&
                        log.Module == "ConfiguracionInventarios" &&
                        log.UserName == "juan.zapata")),
                Times.Once);
        }

        [Fact]
        public async Task Create_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new CreateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            _inventoryConfigurationApplicationMock
                .Setup(x => x.Create(request))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .Create(
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // CREATE ASSIGNMENTS
        // =========================================================

        [Fact]
        public async Task CreateAssignments_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var request =
                new CreateInventoryConfigurationAssignmentsDto();

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateAssignments(
                    0,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAssignments_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateAssignments(
                    1,
                    null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAssignments_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new CreateInventoryConfigurationAssignmentsDto();

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .CreateAssignments(
                    1,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task CreateAssignments_ShouldReturnBadRequest_WhenValidationFails()
        {
            var request =
                new CreateInventoryConfigurationAssignmentsDto();

            _createAssignmentsValidatorMock
                .Setup(x => x.ValidateAsync(
                    request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateInvalidValidationResult(
                        "Assignments",
                        "Asignaciones inválidas."));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateAssignments(
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAssignments_ShouldReturnBadRequest_WhenNoAssignmentsAreCreated()
        {
            var request =
                new CreateInventoryConfigurationAssignmentsDto();

            _inventoryConfigurationApplicationMock
                .Setup(x => x.CreateAssignments(1, request))
                .ReturnsAsync(0);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateAssignments(
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAssignments_ShouldReturnOk_WhenAssignmentsAreCreated()
        {
            var request =
                new CreateInventoryConfigurationAssignmentsDto();

            _inventoryConfigurationApplicationMock
                .Setup(x => x.CreateAssignments(1, request))
                .ReturnsAsync(2);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateAssignments(
                    1,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Crear" &&
                        log.Module == "ConfiguracionInventarios")),
                Times.Once);
        }

        [Fact]
        public async Task CreateAssignments_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new CreateInventoryConfigurationAssignmentsDto();

            _inventoryConfigurationApplicationMock
                .Setup(x => x.CreateAssignments(1, request))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateAssignments(
                    1,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // GET OPTIONS
        // =========================================================

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(-1)]
        public async Task GetOptions_ShouldReturnBadRequest_WhenTypeIsInvalid(
            int type)
        {
            var result =
                await _controller.GetOptions(type);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetOptions_ShouldReturnBadRequest_WhenApplicationReturnsNull()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetOptions(1))
                .ReturnsAsync((object?)null);

            var result =
                await _controller.GetOptions(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task GetOptions_ShouldReturnOk_WhenTypeIsValid(
            int type)
        {
            var options =
                new List<string>
                {
                    "Option 1"
                };

            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetOptions(type))
                .ReturnsAsync(options);

            var result =
                await _controller.GetOptions(type);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);
        }

        [Fact]
        public async Task GetOptions_ShouldReturn500_WhenApplicationThrowsException()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetOptions(1))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.GetOptions(1);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // GET SOLUTION CENTER WITH SECTIONS
        // =========================================================

        [Fact]
        public async Task GetSolutionCenterWithSections_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var result =
                await _controller
                    .GetSolutionCenterWithSections(0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetSolutionCenterWithSections_ShouldReturnNotFound_WhenSolutionCenterDoesNotExist()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetSolutionCenterWithSections(999))
                .ReturnsAsync((SolutionCenterSectionsDto?)null);

            var result =
                await _controller
                    .GetSolutionCenterWithSections(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetSolutionCenterWithSections_ShouldReturnOk_WhenSolutionCenterExists()
        {
            var dto =
                new SolutionCenterSectionsDto
                {
                    SolutionCenterId = 1,
                    SolutionCenterName = "Perecederos"
                };

            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetSolutionCenterWithSections(1))
                .ReturnsAsync(dto);

            var result =
                await _controller
                    .GetSolutionCenterWithSections(1);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);
            Assert.Same(dto, response.Result);
        }

        [Fact]
        public async Task GetSolutionCenterWithSections_ShouldReturn500_WhenApplicationThrowsException()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetSolutionCenterWithSections(1))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller
                    .GetSolutionCenterWithSections(1);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // GET SECTION WITH POINT OF SALES
        // =========================================================

        [Fact]
        public async Task GetSectionWithPointOfSales_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var result =
                await _controller
                    .GetSectionWithPointOfSales(0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetSectionWithPointOfSales_ShouldReturnNotFound_WhenSectionDoesNotExist()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetSectionWithPointOfSales(999))
                .ReturnsAsync((SectionSolutionCentersDto?)null);

            var result =
                await _controller
                    .GetSectionWithPointOfSales(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetSectionWithPointOfSales_ShouldReturnOk_WhenSectionExists()
        {
            var dto =
                new SectionSolutionCentersDto
                {
                    SectionId = 2,
                    SectionName = "Linea de sal"
                };

            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetSectionWithPointOfSales(2))
                .ReturnsAsync(dto);

            var result =
                await _controller
                    .GetSectionWithPointOfSales(2);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);
            Assert.Same(dto, response.Result);
        }

        [Fact]
        public async Task GetSectionWithPointOfSales_ShouldReturn500_WhenApplicationThrowsException()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetSectionWithPointOfSales(2))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller
                    .GetSectionWithPointOfSales(2);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // AVAILABLE INVENTORY CONFIGURATIONS
        // =========================================================

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetAvailableInventoryConfigurations_ShouldReturn400_WhenIdIsInvalid(long solutionCenterId)
        {
            _controller.WithIdentity();

            var result = await _controller.GetAvailableInventoryConfigurations(solutionCenterId);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAvailableInventoryConfigurations_ShouldReturn403_WhenRoleClaimIsMissingOrEmpty(string? role)
        {
            _controller.WithIdentity(role: role);
            _controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");
            _controller.Request.Headers["role"] = "ADMINISTRADOR";
            _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                """{"role":"ADMINISTRADOR"}"""));

            var result = await _controller.GetAvailableInventoryConfigurations(2);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.Equal("El token no contiene un nameRole válido.", response.Message);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAvailableInventoryConfigurations_ShouldReturn404_WhenApplicationReportsMissingCenter()
        {
            _controller.WithIdentity(role: "COSTOS");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryConfigurations(2, "COSTOS"))
                .ReturnsAsync(new AvailableInventoryConfigurationsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = true,
                    Data = null
                });

            var result = await _controller.GetAvailableInventoryConfigurations(2);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<NotFoundObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("La bodega o punto de venta no existe.", response.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAvailableInventoryConfigurations_ShouldReturn403_WhenApplicationDeniesAccess(bool isValidRole)
        {
            _controller.WithIdentity(role: "ALMACEN");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryConfigurations(2, "ALMACEN"))
                .ReturnsAsync(new AvailableInventoryConfigurationsResultDto
                {
                    IsValidRole = isValidRole,
                    IsAllowed = false
                });

            var result = await _controller.GetAvailableInventoryConfigurations(2);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal(isValidRole
                ? "El rol no tiene permisos para consultar este centro."
                : "El rol del token no tiene permisos para esta consulta.", response.Message);
        }

        [Fact]
        public async Task GetAvailableInventoryConfigurations_ShouldReturn200WithIsSuccessFalse_WhenListIsEmpty()
        {
            _controller.WithIdentity(role: "COSTOS");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryConfigurations(2, "COSTOS"))
                .ReturnsAsync(new AvailableInventoryConfigurationsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = true,
                    Data = new List<AvailableInventoryConfigurationDto>()
                });

            var result = await _controller.GetAvailableInventoryConfigurations(2);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("No hay inventarios disponibles para este centro en la fecha actual.", response.Message);
            Assert.Empty(Assert.IsType<List<AvailableInventoryConfigurationDto>>(response.Result));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task GetAvailableInventoryConfigurations_ShouldReturnApplicationList_WhenRecordsExist(int count)
        {
            _controller.WithIdentity(role: "COSTOS");
            var configurations = Enumerable.Range(1, count)
                .Select(id => new AvailableInventoryConfigurationDto
                {
                    InventoryConfigurationId = id,
                    InventoryConfigurationName = $"Inventario {id}"
                }).ToList();
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryConfigurations(2, "COSTOS"))
                .ReturnsAsync(new AvailableInventoryConfigurationsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = true,
                    Data = configurations
                });

            var result = await _controller.GetAvailableInventoryConfigurations(2);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.IsSuccess);
            Assert.Equal("Inventarios disponibles consultados correctamente.", response.Message);
            Assert.Same(configurations, response.Result);
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("ALMACEN")]
        [InlineData("Administrador")]
        public async Task GetAvailableInventoryConfigurations_ShouldUseOnlyClaimAndRoute_IgnoringSuppliedRoleUserAndDate(string role)
        {
            _controller.WithIdentity(role: role);
            _controller.Request.QueryString = new QueryString(
                "?role=SUPLANTADO&userLogin=otro&fecha=2099-01-01&dia=Lunes");
            _controller.Request.Headers["role"] = "SUPLANTADO";
            _controller.Request.Headers["X-User"] = "otro";
            _controller.Request.ContentType = "application/json";
            _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                """{"role":"SUPLANTADO","userLogin":"otro","fecha":"2099-01-01","dia":"Lunes"}"""));
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryConfigurations(2, role))
                .ReturnsAsync(new AvailableInventoryConfigurationsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = true,
                    Data = new List<AvailableInventoryConfigurationDto>()
                });

            Assert.IsType<OkObjectResult>(await _controller.GetAvailableInventoryConfigurations(2));

            _inventoryConfigurationApplicationMock.Verify(
                x => x.GetAvailableInventoryConfigurations(2, role), Times.Once);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAvailableInventoryConfigurations_ShouldReturn500_WhenApplicationThrows()
        {
            _controller.WithIdentity(role: "COSTOS");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryConfigurations(2, "COSTOS"))
                .ThrowsAsync(new InvalidOperationException("Detalle interno del error."));

            var result = await _controller.GetAvailableInventoryConfigurations(2);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("Ocurrió un error al consultar los inventarios disponibles.", response.Message);
            _logApplicationMock.VerifyNoOtherCalls();
        }

        // =========================================================
        // AVAILABLE INVENTORY SECTIONS
        // =========================================================

        private static AvailableInventorySectionsResultDto CreateAvailableSectionsResult()
        {
            return new AvailableInventorySectionsResultDto
            {
                IsValidRole = true,
                IsAllowed = true,
                SolutionCenterExists = true,
                ConfigurationExists = true,
                IsAvailable = true
            };
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(2, 0)]
        [InlineData(2, -1)]
        public async Task GetAvailableInventorySections_ShouldReturn400_WhenEitherIdIsInvalid(
            long solutionCenterId, long inventoryConfigurationId)
        {
            _controller.WithIdentity();

            var result = await _controller.GetAvailableInventorySections(solutionCenterId, inventoryConfigurationId);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal(solutionCenterId <= 0
                ? "El identificador de la bodega o punto de venta debe ser mayor a cero."
                : "El identificador de la configuración de inventario debe ser mayor a cero.", response.Message);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAvailableInventorySections_ShouldReturn403_WhenRoleClaimIsMissingOrEmpty(string? role)
        {
            _controller.WithIdentity(role: role);
            _controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");
            _controller.Request.Headers["role"] = "ADMINISTRADOR";
            _controller.Request.ContentType = "application/json";
            _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                """{"role":"ADMINISTRADOR"}"""));

            var result = await _controller.GetAvailableInventorySections(2, 1);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("El token no contiene un nameRole válido.", response.Message);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAvailableInventorySections_ShouldReturn404_WhenApplicationReportsMissingCenter()
        {
            _controller.WithIdentity(role: "COSTOS");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventorySections(2, 1, "COSTOS"))
                .ReturnsAsync(new AvailableInventorySectionsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = true,
                    SolutionCenterExists = false
                });

            var result = await _controller.GetAvailableInventorySections(2, 1);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<NotFoundObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("La bodega o punto de venta no existe.", response.Message);
        }

        [Fact]
        public async Task GetAvailableInventorySections_ShouldReturn404_WhenApplicationReportsMissingConfiguration()
        {
            _controller.WithIdentity(role: "COSTOS");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventorySections(2, 1, "COSTOS"))
                .ReturnsAsync(new AvailableInventorySectionsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = true,
                    SolutionCenterExists = true,
                    ConfigurationExists = false
                });

            var result = await _controller.GetAvailableInventorySections(2, 1);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<NotFoundObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("La configuración de inventario no existe.", response.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAvailableInventorySections_ShouldReturn403_WhenApplicationDeniesAccess(bool isValidRole)
        {
            _controller.WithIdentity(role: "ALMACEN");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventorySections(2, 1, "ALMACEN"))
                .ReturnsAsync(new AvailableInventorySectionsResultDto
                {
                    IsValidRole = isValidRole,
                    IsAllowed = false
                });

            var result = await _controller.GetAvailableInventorySections(2, 1);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal(isValidRole
                ? "El rol no tiene permisos para consultar este centro."
                : "El rol del token no tiene permisos para esta consulta.", response.Message);
        }

        [Fact]
        public async Task GetAvailableInventorySections_ShouldReturn404_WhenConfigurationIsUnavailable()
        {
            _controller.WithIdentity(role: "COSTOS");
            var applicationResult = CreateAvailableSectionsResult();
            applicationResult.IsAvailable = false;
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventorySections(2, 1, "COSTOS"))
                .ReturnsAsync(applicationResult);

            var result = await _controller.GetAvailableInventorySections(2, 1);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<NotFoundObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("La configuración de inventario no está disponible para este centro.", response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
        }

        [Fact]
        public async Task GetAvailableInventorySections_ShouldReturn200WithIsSuccessFalse_WhenListIsEmpty()
        {
            _controller.WithIdentity(role: "COSTOS");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventorySections(2, 1, "COSTOS"))
                .ReturnsAsync(CreateAvailableSectionsResult());

            var result = await _controller.GetAvailableInventorySections(2, 1);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("No hay secciones disponibles para esta configuración.", response.Message);
            Assert.Empty(Assert.IsType<List<AvailableInventorySectionDto>>(response.Result));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task GetAvailableInventorySections_ShouldReturnApplicationList_WhenRecordsExist(int count)
        {
            _controller.WithIdentity(role: "COSTOS");
            var applicationResult = CreateAvailableSectionsResult();
            applicationResult.Data = Enumerable.Range(1, count)
                .Select(id => new AvailableInventorySectionDto
                {
                    SectionId = id,
                    SectionName = $"Sección {id}"
                }).ToList();
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventorySections(2, 1, "COSTOS"))
                .ReturnsAsync(applicationResult);

            var result = await _controller.GetAvailableInventorySections(2, 1);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.IsSuccess);
            Assert.Equal("Secciones disponibles consultadas correctamente.", response.Message);
            Assert.Same(applicationResult.Data, response.Result);
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAvailableInventorySections_ShouldReturn500_WhenApplicationThrows()
        {
            _controller.WithIdentity(role: "COSTOS");
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventorySections(2, 1, "COSTOS"))
                .ThrowsAsync(new InvalidOperationException("Detalle interno del error."));

            var result = await _controller.GetAvailableInventorySections(2, 1);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("Ocurrió un error al consultar las secciones disponibles.", response.Message);
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("ALMACEN")]
        [InlineData("Administrador")]
        public async Task GetAvailableInventorySections_ShouldUseOnlyClaimAndRoute_IgnoringAlternativeInputs(string role)
        {
            _controller.WithIdentity(role: role);
            _controller.Request.QueryString = new QueryString(
                "?role=SUPLANTADO&userLogin=otro&sectionId=99&fecha=2099-01-01&dia=Lunes");
            _controller.Request.Headers["role"] = "SUPLANTADO";
            _controller.Request.Headers["X-User"] = "otro";
            _controller.Request.ContentType = "application/json";
            _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                """{"role":"SUPLANTADO","userLogin":"otro","sectionId":99,"fecha":"2099-01-01","dia":"Lunes"}"""));
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventorySections(2, 1, role))
                .ReturnsAsync(CreateAvailableSectionsResult());

            Assert.IsType<OkObjectResult>(await _controller.GetAvailableInventorySections(2, 1));

            _inventoryConfigurationApplicationMock.Verify(
                x => x.GetAvailableInventorySections(2, 1, role), Times.Once);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        // =========================================================
        // AVAILABLE INVENTORY PRODUCTS
        // =========================================================

        private static AvailableInventoryProductsResultDto CreateAvailableProductsResult(int page = 1, int take = 20)
        {
            return new AvailableInventoryProductsResultDto
            {
                IsValidRole = true,
                IsAllowed = true,
                HasDetailPermission = true,
                SolutionCenterExists = true,
                ConfigurationExists = true,
                IsAvailable = true,
                IsSectionAvailable = true,
                Data = new PagedDto<AvailableInventoryProductDto>
                {
                    Page = page,
                    Take = take
                }
            };
        }

        [Theory]
        [InlineData(0, 1, 2, "El identificador de la bodega o punto de venta debe ser mayor a cero.")]
        [InlineData(-1, 1, 2, "El identificador de la bodega o punto de venta debe ser mayor a cero.")]
        [InlineData(2, 0, 2, "El identificador de la configuración de inventario debe ser mayor a cero.")]
        [InlineData(2, -1, 2, "El identificador de la configuración de inventario debe ser mayor a cero.")]
        [InlineData(2, 1, 0, "El identificador de la sección debe ser mayor a cero.")]
        [InlineData(2, 1, -1, "El identificador de la sección debe ser mayor a cero.")]
        public async Task GetAvailableInventoryProducts_ShouldReturn400_WhenAnyIdIsInvalid(
            long solutionCenterId, long inventoryConfigurationId, long sectionId, string expectedMessage)
        {
            _controller.WithIdentity();

            var result = await _controller.GetAvailableInventoryProducts(
                solutionCenterId, inventoryConfigurationId, sectionId);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(0, 20)]
        [InlineData(-1, 20)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public async Task GetAvailableInventoryProducts_ShouldReturn400_WhenPaginationIsInvalid(int page, int take)
        {
            _controller.WithIdentity();

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2, page, take);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("Page y Take deben ser mayores a cero.", response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAvailableInventoryProducts_ShouldReturn403_WhenRoleClaimIsMissingOrEmpty(string? role)
        {
            _controller.WithIdentity(role: role);
            _controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR&nameRole=ADMINISTRADOR");
            _controller.Request.Headers["role"] = "ADMINISTRADOR";
            _controller.Request.Headers["nameRole"] = "ADMINISTRADOR";
            _controller.Request.ContentType = "application/json";
            _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                """{"role":"ADMINISTRADOR","nameRole":"ADMINISTRADOR"}"""));

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("El token no contiene un nameRole válido.", response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetAvailableInventoryProducts_ShouldReturn403_WhenRoleClaimIsAmbiguousOrUnauthenticated(
            bool isAuthenticated)
        {
            _controller.WithIdentity();
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("nameRole", "COSTOS")
            };
            if (isAuthenticated)
                claims.Add(new System.Security.Claims.Claim("nameRole", "ADMINISTRADOR"));
            _controller.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(claims, isAuthenticated ? "Bearer" : null));

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("El token no contiene un nameRole válido.", response.Message);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false, false, false, "El rol del token no tiene permisos para esta consulta.")]
        [InlineData(true, false, false, "El rol no tiene permisos para consultar este centro.")]
        [InlineData(true, true, false, "El rol no tiene permisos para consultar el detalle del inventario.")]
        public async Task GetAvailableInventoryProducts_ShouldReturn403_WhenApplicationDeniesAccess(
            bool isValidRole, bool isAllowed, bool hasDetailPermission, string expectedMessage)
        {
            _controller.WithIdentity();
            var applicationResult = CreateAvailableProductsResult();
            applicationResult.IsValidRole = isValidRole;
            applicationResult.IsAllowed = isAllowed;
            applicationResult.HasDetailPermission = hasDetailPermission;
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 1, 20))
                .ReturnsAsync(applicationResult);

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
        }

        [Theory]
        [InlineData(false, false, false, false, "La bodega o punto de venta no existe.")]
        [InlineData(true, false, false, false, "La configuración de inventario no existe.")]
        [InlineData(true, true, false, false, "La configuración de inventario no está disponible para este centro.")]
        [InlineData(true, true, true, false, "La sección no está disponible para esta configuración y centro.")]
        public async Task GetAvailableInventoryProducts_ShouldReturn404_WhenApplicationReportsUnavailableChain(
            bool solutionCenterExists, bool configurationExists, bool isAvailable, bool isSectionAvailable,
            string expectedMessage)
        {
            _controller.WithIdentity();
            var applicationResult = CreateAvailableProductsResult();
            applicationResult.SolutionCenterExists = solutionCenterExists;
            applicationResult.ConfigurationExists = configurationExists;
            applicationResult.IsAvailable = isAvailable;
            applicationResult.IsSectionAvailable = isSectionAvailable;
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 1, 20))
                .ReturnsAsync(applicationResult);

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<NotFoundObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
        }

        [Fact]
        public async Task GetAvailableInventoryProducts_ShouldReturnEmptyPageAndUseDefaults_WhenPaginationIsOmitted()
        {
            _controller.WithIdentity();
            var applicationResult = CreateAvailableProductsResult();
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 1, 20))
                .ReturnsAsync(applicationResult);

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("No hay productos disponibles para esta sección.", response.Message);
            var page = Assert.IsType<PagedDto<AvailableInventoryProductDto>>(response.Result);
            Assert.Same(applicationResult.Data, page);
            Assert.Empty(page.Items);
            Assert.Equal(0, page.Total);
            Assert.Equal(1, page.Page);
            Assert.Equal(20, page.Take);
            Assert.Equal(0, page.Pages);
            _inventoryConfigurationApplicationMock.Verify(
                x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 1, 20), Times.Once);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task GetAvailableInventoryProducts_ShouldReturnApplicationPageAndForwardPagination_WhenProductsExist(
            int count)
        {
            _controller.WithIdentity();
            var applicationResult = CreateAvailableProductsResult(page: 2, take: 3);
            applicationResult.Data.Items = Enumerable.Range(1, count)
                .Select(id => new AvailableInventoryProductDto
                {
                    SolutionCenterProductId = id,
                    ProductId = id + 10,
                    ImagePath = null,
                    Reference = $"REF{id}",
                    ProductName = $"Producto {id}",
                    UnitOfMeasure = "Mililitros",
                    PlanId = "FAC",
                    SortOrder = id
                }).ToList();
            applicationResult.Data.Total = 3 + count;
            applicationResult.Data.Pages = 2;
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 2, 3))
                .ReturnsAsync(applicationResult);

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2, page: 2, take: 3);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.IsSuccess);
            Assert.Equal("Productos de la sección consultados correctamente.", response.Message);
            var page = Assert.IsType<PagedDto<AvailableInventoryProductDto>>(response.Result);
            Assert.Same(applicationResult.Data, page);
            Assert.Equal(count, page.Items.Count());
            Assert.Equal(3 + count, page.Total);
            Assert.Equal(2, page.Page);
            Assert.Equal(3, page.Take);
            Assert.Equal(2, page.Pages);
            _inventoryConfigurationApplicationMock.Verify(
                x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 2, 3), Times.Once);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(
            "sitracalco_inventory_dev/image/products/10/abc.jpg",
            "https://storage.googleapis.com/inventory-test/sitracalco_inventory_dev/image/products/10/abc.jpg?X-Goog-Expires=900&X-Goog-Signature=test-signature")]
        [InlineData(null, null)]
        public async Task GetAvailableInventoryProducts_ShouldPreserveImageFieldsFromApplication(
            string? imagePath, string? imageUrl)
        {
            _controller.WithIdentity();
            var applicationResult = CreateAvailableProductsResult();
            applicationResult.Data.Items = new List<AvailableInventoryProductDto>
            {
                new AvailableInventoryProductDto
                {
                    SolutionCenterProductId = 3,
                    ProductId = 10,
                    ImagePath = imagePath,
                    ImageUrl = imageUrl,
                    Reference = "0001901",
                    ProductName = "Anfora Real Tostado",
                    UnitOfMeasure = "Mililitros",
                    PlanId = "FAC",
                    SortOrder = 1
                }
            };
            applicationResult.Data.Total = 1;
            applicationResult.Data.Pages = 1;
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 1, 20))
                .ReturnsAsync(applicationResult);

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2);

            var response = Assert.IsType<ResponseApi>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(response.IsSuccess);
            Assert.Equal("Productos de la sección consultados correctamente.", response.Message);
            var page = Assert.IsType<PagedDto<AvailableInventoryProductDto>>(response.Result);
            Assert.Same(applicationResult.Data, page);
            var product = Assert.Single(page.Items);
            Assert.Equal(imagePath, product.ImagePath);
            Assert.Equal(imageUrl, product.ImageUrl);
            Assert.Equal(3, product.SolutionCenterProductId);
            Assert.Equal(10, product.ProductId);
            Assert.Equal("0001901", product.Reference);
            Assert.Equal("Anfora Real Tostado", product.ProductName);
            Assert.Equal("Mililitros", product.UnitOfMeasure);
            Assert.Equal("FAC", product.PlanId);
            Assert.Equal(1, product.SortOrder);
            Assert.Equal(1, page.Total);
            Assert.Equal(1, page.Page);
            Assert.Equal(20, page.Take);
            Assert.Equal(1, page.Pages);
            _inventoryConfigurationApplicationMock.Verify(
                x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 1, 20), Times.Once);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
            _logApplicationMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("ALMACEN")]
        [InlineData("Administrador")]
        public async Task GetAvailableInventoryProducts_ShouldUseOnlyClaimAndRoute_IgnoringAlternativeInputs(string role)
        {
            _controller.WithIdentity(role: role);
            _controller.Request.QueryString = new QueryString(
                "?role=SUPLANTADO&nameRole=SUPLANTADO&userLogin=otro&solutionCenterId=99&inventoryConfigurationId=99&sectionId=99&fecha=2099-01-01&dia=Lunes");
            _controller.Request.Headers["role"] = "SUPLANTADO";
            _controller.Request.Headers["nameRole"] = "SUPLANTADO";
            _controller.Request.Headers["X-User"] = "otro";
            _controller.Request.ContentType = "application/json";
            _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                """{"role":"SUPLANTADO","nameRole":"SUPLANTADO","userLogin":"otro","solutionCenterId":99,"inventoryConfigurationId":99,"sectionId":99,"fecha":"2099-01-01","dia":"Lunes"}"""));
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryProducts(2, 1, 2, role, 1, 20))
                .ReturnsAsync(CreateAvailableProductsResult());

            Assert.IsType<OkObjectResult>(await _controller.GetAvailableInventoryProducts(2, 1, 2));

            _inventoryConfigurationApplicationMock.Verify(
                x => x.GetAvailableInventoryProducts(2, 1, 2, role, 1, 20), Times.Once);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetAvailableInventoryProducts_ShouldReturn500_WhenApplicationThrows()
        {
            _controller.WithIdentity();
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetAvailableInventoryProducts(2, 1, 2, "COSTOS", 1, 20))
                .ThrowsAsync(new InvalidOperationException("Detalle interno del error."));

            var result = await _controller.GetAvailableInventoryProducts(2, 1, 2);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Equal("Ocurrió un error al consultar los productos de la sección.", response.Message);
            Assert.Empty(response.Result.GetType().GetProperties());
            _logApplicationMock.VerifyNoOtherCalls();
        }

        // =========================================================
        // GET CONFIGURATIONS BY SOLUTION CENTER
        // =========================================================

        [Fact]
        public async Task RoleQueries_ShouldUseClaims_WhenQueryContainsAnotherRole()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetInventoryConfigurationsBySolutionCenterId(1, "ALMACEN"))
                .ReturnsAsync(new SolutionCenterInventoryConfigurationsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = false
                });
            _inventoryConfigurationApplicationMock
                .Setup(x => x.GetInventoryConfigurationById(1, "ALMACEN"))
                .ReturnsAsync(new InventoryConfigurationByIdResultDto
                {
                    IsValidRole = true,
                    Data = null
                });
            _controller.WithIdentity(role: "ALMACEN");
            _controller.Request.QueryString = new QueryString("?role=ADMINISTRADOR");

            var byCenter = await _controller.GetInventoryConfigurationsBySolutionCenterId(1);
            var byId = await _controller.GetInventoryConfigurationById(1);

            Assert.Equal(403, Assert.IsType<ObjectResult>(byCenter).StatusCode);
            Assert.IsType<NotFoundObjectResult>(byId);
            _inventoryConfigurationApplicationMock.Verify(
                x => x.GetInventoryConfigurationsBySolutionCenterId(1, "ALMACEN"), Times.Once);
            _inventoryConfigurationApplicationMock.Verify(
                x => x.GetInventoryConfigurationById(1, "ALMACEN"), Times.Once);
            _inventoryConfigurationApplicationMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetInventoryConfigurationsBySolutionCenterId(
                    0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturnForbidden_WhenRoleClaimIsEmpty()
        {
            var result =
                await _controller.WithIdentity(role: "")
                    .GetInventoryConfigurationsBySolutionCenterId(
                    1);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturnForbidden_WhenRoleIsInvalid()
        {
            var applicationResult =
                new SolutionCenterInventoryConfigurationsResultDto
                {
                    IsValidRole = false,
                    IsAllowed = false
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationsBySolutionCenterId(
                        1,
                        "INVALIDO"))
                .ReturnsAsync(applicationResult);

            var result =
                await _controller.WithIdentity(role: "INVALIDO")
                    .GetInventoryConfigurationsBySolutionCenterId(
                    1);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturn403_WhenRoleIsNotAllowed()
        {
            var applicationResult =
                new SolutionCenterInventoryConfigurationsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = false
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationsBySolutionCenterId(
                        2,
                        "ALMACEN"))
                .ReturnsAsync(applicationResult);

            var result =
                await _controller.WithIdentity(role: "ALMACEN")
                    .GetInventoryConfigurationsBySolutionCenterId(
                    2);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status403Forbidden,
                objectResult.StatusCode);
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturnNotFound_WhenSolutionCenterDoesNotExist()
        {
            var applicationResult =
                new SolutionCenterInventoryConfigurationsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = true,
                    Data = null
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationsBySolutionCenterId(
                        999,
                        "COSTOS"))
                .ReturnsAsync(applicationResult);

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetInventoryConfigurationsBySolutionCenterId(
                    999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturnOk_WhenRequestIsValid()
        {
            var data =
                new SolutionCenterInventoryConfigurationsDto
                {
                    SolutionCenterId = 1,
                    SolutionCenterName = "Perecederos"
                };

            var applicationResult =
                new SolutionCenterInventoryConfigurationsResultDto
                {
                    IsValidRole = true,
                    IsAllowed = true,
                    Data = data
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationsBySolutionCenterId(
                        1,
                        "COSTOS"))
                .ReturnsAsync(applicationResult);

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetInventoryConfigurationsBySolutionCenterId(
                    1);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);
            Assert.Same(data, response.Result);
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturn500_WhenApplicationThrowsException()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationsBySolutionCenterId(
                        1,
                        "COSTOS"))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetInventoryConfigurationsBySolutionCenterId(
                    1);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // GET INVENTORY CONFIGURATION BY ID
        // =========================================================

        [Fact]
        public async Task GetInventoryConfigurationById_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetInventoryConfigurationById(
                    0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetInventoryConfigurationById_ShouldReturnForbidden_WhenRoleClaimIsEmpty()
        {
            var result =
                await _controller.WithIdentity(role: "")
                    .GetInventoryConfigurationById(
                    1);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task GetInventoryConfigurationById_ShouldReturnForbidden_WhenRoleIsInvalid()
        {
            var applicationResult =
                new InventoryConfigurationByIdResultDto
                {
                    IsValidRole = false,
                    Data = null
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationById(
                        1,
                        "INVALIDO"))
                .ReturnsAsync(applicationResult);

            var result =
                await _controller.WithIdentity(role: "INVALIDO")
                    .GetInventoryConfigurationById(
                    1);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task GetInventoryConfigurationById_ShouldReturnNotFound_WhenConfigurationDoesNotExist()
        {
            var applicationResult =
                new InventoryConfigurationByIdResultDto
                {
                    IsValidRole = true,
                    Data = null
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationById(
                        999,
                        "COSTOS"))
                .ReturnsAsync(applicationResult);

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetInventoryConfigurationById(
                    999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetInventoryConfigurationById_ShouldReturnOk_WhenConfigurationExists()
        {
            var data =
                new InventoryConfigurationByIdDto
                {
                    InventoryConfigurationId = 1,
                    InventoryConfigurationName = "Uno a uno"
                };

            var applicationResult =
                new InventoryConfigurationByIdResultDto
                {
                    IsValidRole = true,
                    Data = data
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationById(
                        1,
                        "COSTOS"))
                .ReturnsAsync(applicationResult);

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetInventoryConfigurationById(
                    1);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);
            Assert.Same(data, response.Result);
        }

        [Fact]
        public async Task GetInventoryConfigurationById_ShouldReturn500_WhenApplicationThrowsException()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.GetInventoryConfigurationById(
                        1,
                        "COSTOS"))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetInventoryConfigurationById(
                    1);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // UPDATE ASSIGNMENT STATUS
        // =========================================================

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturnBadRequest_WhenIdsAreInvalid()
        {
            var request =
                new UpdateInventoryConfigurationAssignmentStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateAssignmentStatus(
                    0,
                    1,
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateAssignmentStatus(
                    1,
                    1,
                    1,
                    null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new UpdateInventoryConfigurationAssignmentStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .UpdateAssignmentStatus(
                    1,
                    1,
                    1,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturnBadRequest_WhenValidationFails()
        {
            var request =
                new UpdateInventoryConfigurationAssignmentStatusDto();

            _updateAssignmentStatusValidatorMock
                .Setup(x => x.ValidateAsync(
                    request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateInvalidValidationResult(
                        "IsActive",
                        "Estado obligatorio."));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateAssignmentStatus(
                    1,
                    1,
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
        {
            var request =
                new UpdateInventoryConfigurationAssignmentStatusDto
                {
                    IsActive = false
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.UpdateAssignmentStatus(
                        1,
                        2,
                        2,
                        false))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateAssignmentStatus(
                    1,
                    2,
                    2,
                    request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturnOk_WhenAssignmentIsUpdated()
        {
            var request =
                new UpdateInventoryConfigurationAssignmentStatusDto
                {
                    IsActive = false
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.UpdateAssignmentStatus(
                        1,
                        2,
                        2,
                        false))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateAssignmentStatus(
                    1,
                    2,
                    2,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Asignación inactivada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Actualizar" &&
                        log.Module == "ConfiguracionInventarios")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new UpdateInventoryConfigurationAssignmentStatusDto
                {
                    IsActive = true
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.UpdateAssignmentStatus(
                        1,
                        2,
                        2,
                        true))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateAssignmentStatus(
                    1,
                    2,
                    2,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // UPDATE INVENTORY CONFIGURATION
        // =========================================================

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var request =
                new UpdateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateInventoryConfiguration(
                    0,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateInventoryConfiguration(
                    1,
                    null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new UpdateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .UpdateInventoryConfiguration(
                    1,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturnBadRequest_WhenValidationFails()
        {
            var request =
                new UpdateInventoryConfigurationDto
                {
                    InventoryConfigurationName = ""
                };

            _updateInventoryConfigurationValidatorMock
                .Setup(x => x.ValidateAsync(
                    request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateInvalidValidationResult(
                        "InventoryConfigurationName",
                        "Nombre obligatorio."));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateInventoryConfiguration(
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturnBadRequest_WhenApplicationReturnsFalse()
        {
            var request =
                new UpdateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.UpdateInventoryConfiguration(
                        1,
                        request))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateInventoryConfiguration(
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturnOk_WhenConfigurationIsUpdated()
        {
            var request =
                new UpdateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno",
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 30)
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.UpdateInventoryConfiguration(
                        1,
                        request))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateInventoryConfiguration(
                    1,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Actualizar" &&
                        log.Module == "ConfiguracionInventarios")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new UpdateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.UpdateInventoryConfiguration(
                        1,
                        request))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateInventoryConfiguration(
                    1,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // ADD DAYS
        // =========================================================

        [Fact]
        public async Task AddDays_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var request =
                new AddInventoryConfigurationDaysDto
                {
                    Days = new List<string>
                    {
                        "Martes"
                    }
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddDays(
                    0,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddDays_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddDays(
                    1,
                    null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddDays_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new AddInventoryConfigurationDaysDto
                {
                    Days = new List<string>
                    {
                        "Martes"
                    }
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .AddDays(
                    1,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task AddDays_ShouldReturnBadRequest_WhenValidationFails()
        {
            var request =
                new AddInventoryConfigurationDaysDto
                {
                    Days = new List<string>()
                };

            _addInventoryConfigurationDaysValidatorMock
                .Setup(x => x.ValidateAsync(
                    request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateInvalidValidationResult(
                        "Days",
                        "Debe enviar al menos un día."));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddDays(
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddDays_ShouldReturnBadRequest_WhenNoDaysAreCreated()
        {
            var request =
                new AddInventoryConfigurationDaysDto
                {
                    Days = new List<string>
                    {
                        "Martes"
                    }
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.AddDays(
                        1,
                        request))
                .ReturnsAsync(0);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddDays(
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddDays_ShouldReturnOk_WhenDaysAreCreated()
        {
            var request =
                new AddInventoryConfigurationDaysDto
                {
                    Days = new List<string>
                    {
                        "Martes",
                        "Miercoles"
                    }
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.AddDays(
                        1,
                        request))
                .ReturnsAsync(2);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddDays(
                    1,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Días agregados correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Crear" &&
                        log.Module == "ConfiguracionInventarios")),
                Times.Once);
        }

        [Fact]
        public async Task AddDays_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new AddInventoryConfigurationDaysDto
                {
                    Days = new List<string>
                    {
                        "Martes"
                    }
                };

            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.AddDays(
                        1,
                        request))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddDays(
                    1,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // DELETE DAY
        // =========================================================

        [Fact]
        public async Task DeleteDay_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteDay(
                    0,
                    "Martes");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDay_ShouldReturnBadRequest_WhenDayIsEmpty()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteDay(
                    1,
                    "");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDay_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var result =
                await _controller.WithIdentity(userLogin: "")
                    .DeleteDay(
                    1,
                    "Martes");

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task DeleteDay_ShouldReturnNotFound_WhenDayDoesNotExist()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteDay(
                        1,
                        "Martes"))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteDay(
                    1,
                    "Martes");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDay_ShouldReturnOk_WhenDayIsDeleted()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteDay(
                        1,
                        "Martes"))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteDay(
                    1,
                    "Martes");

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Día eliminado correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Eliminar")),
                Times.Once);
        }

        [Fact]
        public async Task DeleteDay_ShouldReturn500_WhenApplicationThrowsException()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteDay(
                        1,
                        "Martes"))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteDay(
                    1,
                    "Martes");

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // DELETE SPECIFIC ASSIGNMENT
        // =========================================================

        [Fact]
        public async Task DeleteAssignment_ShouldReturnBadRequest_WhenIdsAreInvalid()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteAssignment(
                    0,
                    2,
                    2);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAssignment_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var result =
                await _controller.WithIdentity(userLogin: "")
                    .DeleteAssignment(
                    1,
                    2,
                    2);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task DeleteAssignment_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteAssignment(
                        1,
                        2,
                        2))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteAssignment(
                    1,
                    2,
                    2);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAssignment_ShouldReturnOk_WhenAssignmentIsDeleted()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteAssignment(
                        1,
                        2,
                        2))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteAssignment(
                    1,
                    2,
                    2);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Asignación eliminada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Eliminar")),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAssignment_ShouldReturn500_WhenApplicationThrowsException()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteAssignment(
                        1,
                        2,
                        2))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteAssignment(
                    1,
                    2,
                    2);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }

        // =========================================================
        // DELETE ASSIGNMENTS BY SECTION
        // =========================================================

        [Fact]
        public async Task DeleteAssignmentsBySection_ShouldReturnBadRequest_WhenIdsAreInvalid()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteAssignmentsBySection(
                    0,
                    2);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAssignmentsBySection_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var result =
                await _controller.WithIdentity(userLogin: "")
                    .DeleteAssignmentsBySection(
                    1,
                    2);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task DeleteAssignmentsBySection_ShouldReturnNotFound_WhenNoAssignmentsExist()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteAssignmentsBySection(
                        1,
                        2))
                .ReturnsAsync(0);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteAssignmentsBySection(
                    1,
                    2);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAssignmentsBySection_ShouldReturnOk_WhenAssignmentsAreDeleted()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteAssignmentsBySection(
                        1,
                        2))
                .ReturnsAsync(2);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteAssignmentsBySection(
                    1,
                    2);

            var ok =
                Assert.IsType<OkObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);

            Assert.Equal(
                "Sección eliminada de la configuración de inventario correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Eliminar" &&
                        log.Description.Contains(
                            "2 asociación(es)"))),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAssignmentsBySection_ShouldReturn500_WhenApplicationThrowsException()
        {
            _inventoryConfigurationApplicationMock
                .Setup(x =>
                    x.DeleteAssignmentsBySection(
                        1,
                        2))
                .ThrowsAsync(new Exception("Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteAssignmentsBySection(
                    1,
                    2);

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
