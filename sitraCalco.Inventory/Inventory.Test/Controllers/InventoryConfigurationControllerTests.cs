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
        public async Task Create_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new CreateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            var result =
                await _controller.Create(
                    request,
                    "");

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            var response =
                Assert.IsType<ResponseApi>(badRequest.Value);

            Assert.False(response.IsSuccess);

            Assert.Equal(
                "El usuario que ejecuta la operación es obligatorio.",
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
                await _controller.Create(
                    request,
                    "juan.zapata");

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
                await _controller.Create(
                    request,
                    "juan.zapata");

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
                await _controller.Create(
                    request,
                    "juan.zapata");

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
                await _controller.Create(
                    request,
                    "juan.zapata");

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
                await _controller.CreateAssignments(
                    0,
                    request,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAssignments_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.CreateAssignments(
                    1,
                    null!,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAssignments_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new CreateInventoryConfigurationAssignmentsDto();

            var result =
                await _controller.CreateAssignments(
                    1,
                    request,
                    "");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller.CreateAssignments(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.CreateAssignments(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.CreateAssignments(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.CreateAssignments(
                    1,
                    request,
                    "juan.zapata");

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
        // GET CONFIGURATIONS BY SOLUTION CENTER
        // =========================================================

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var result =
                await _controller
                    .GetInventoryConfigurationsBySolutionCenterId(
                        0,
                        "COSTOS");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturnBadRequest_WhenRoleIsEmpty()
        {
            var result =
                await _controller
                    .GetInventoryConfigurationsBySolutionCenterId(
                        1,
                        "");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetConfigurationsBySolutionCenter_ShouldReturnBadRequest_WhenRoleIsInvalid()
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
                await _controller
                    .GetInventoryConfigurationsBySolutionCenterId(
                        1,
                        "INVALIDO");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller
                    .GetInventoryConfigurationsBySolutionCenterId(
                        2,
                        "ALMACEN");

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
                await _controller
                    .GetInventoryConfigurationsBySolutionCenterId(
                        999,
                        "COSTOS");

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
                await _controller
                    .GetInventoryConfigurationsBySolutionCenterId(
                        1,
                        "COSTOS");

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
                await _controller
                    .GetInventoryConfigurationsBySolutionCenterId(
                        1,
                        "COSTOS");

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
                await _controller
                    .GetInventoryConfigurationById(
                        0,
                        "COSTOS");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetInventoryConfigurationById_ShouldReturnBadRequest_WhenRoleIsEmpty()
        {
            var result =
                await _controller
                    .GetInventoryConfigurationById(
                        1,
                        "");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetInventoryConfigurationById_ShouldReturnBadRequest_WhenRoleIsInvalid()
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
                await _controller
                    .GetInventoryConfigurationById(
                        1,
                        "INVALIDO");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller
                    .GetInventoryConfigurationById(
                        999,
                        "COSTOS");

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
                await _controller
                    .GetInventoryConfigurationById(
                        1,
                        "COSTOS");

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
                await _controller
                    .GetInventoryConfigurationById(
                        1,
                        "COSTOS");

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
                await _controller.UpdateAssignmentStatus(
                    0,
                    1,
                    1,
                    request,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.UpdateAssignmentStatus(
                    1,
                    1,
                    1,
                    null!,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAssignmentStatus_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new UpdateInventoryConfigurationAssignmentStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller.UpdateAssignmentStatus(
                    1,
                    1,
                    1,
                    request,
                    "");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller.UpdateAssignmentStatus(
                    1,
                    1,
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.UpdateAssignmentStatus(
                    1,
                    2,
                    2,
                    request,
                    "juan.zapata");

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
                await _controller.UpdateAssignmentStatus(
                    1,
                    2,
                    2,
                    request,
                    "juan.zapata");

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
                await _controller.UpdateAssignmentStatus(
                    1,
                    2,
                    2,
                    request,
                    "juan.zapata");

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
                await _controller.UpdateInventoryConfiguration(
                    0,
                    request,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.UpdateInventoryConfiguration(
                    1,
                    null!,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateInventoryConfiguration_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new UpdateInventoryConfigurationDto
                {
                    InventoryConfigurationName = "Uno a uno"
                };

            var result =
                await _controller.UpdateInventoryConfiguration(
                    1,
                    request,
                    "");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller.UpdateInventoryConfiguration(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.UpdateInventoryConfiguration(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.UpdateInventoryConfiguration(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.UpdateInventoryConfiguration(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.AddDays(
                    0,
                    request,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddDays_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.AddDays(
                    1,
                    null!,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddDays_ShouldReturnBadRequest_WhenUserIsEmpty()
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
                await _controller.AddDays(
                    1,
                    request,
                    "");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller.AddDays(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.AddDays(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.AddDays(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.AddDays(
                    1,
                    request,
                    "juan.zapata");

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
                await _controller.DeleteDay(
                    0,
                    "Martes",
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDay_ShouldReturnBadRequest_WhenDayIsEmpty()
        {
            var result =
                await _controller.DeleteDay(
                    1,
                    "",
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDay_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var result =
                await _controller.DeleteDay(
                    1,
                    "Martes",
                    "");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller.DeleteDay(
                    1,
                    "Martes",
                    "juan.zapata");

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
                await _controller.DeleteDay(
                    1,
                    "Martes",
                    "juan.zapata");

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
                await _controller.DeleteDay(
                    1,
                    "Martes",
                    "juan.zapata");

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
                await _controller.DeleteAssignment(
                    0,
                    2,
                    2,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAssignment_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var result =
                await _controller.DeleteAssignment(
                    1,
                    2,
                    2,
                    "");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller.DeleteAssignment(
                    1,
                    2,
                    2,
                    "juan.zapata");

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
                await _controller.DeleteAssignment(
                    1,
                    2,
                    2,
                    "juan.zapata");

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
                await _controller.DeleteAssignment(
                    1,
                    2,
                    2,
                    "juan.zapata");

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
                await _controller.DeleteAssignmentsBySection(
                    0,
                    2,
                    "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAssignmentsBySection_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var result =
                await _controller.DeleteAssignmentsBySection(
                    1,
                    2,
                    "");

            Assert.IsType<BadRequestObjectResult>(result);
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
                await _controller.DeleteAssignmentsBySection(
                    1,
                    2,
                    "juan.zapata");

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
                await _controller.DeleteAssignmentsBySection(
                    1,
                    2,
                    "juan.zapata");

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
                await _controller.DeleteAssignmentsBySection(
                    1,
                    2,
                    "juan.zapata");

            var objectResult =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}