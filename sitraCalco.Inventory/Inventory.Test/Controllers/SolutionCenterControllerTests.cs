using Inventory.Test.Helpers;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Models;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Inventory.Test.Controllers
{
    public class SolutionCenterControllerTests
    {
        private readonly Mock<ISolutionCenterApplication>
            _solutionCenterApplicationMock;

        private readonly Mock<ILogApplication>
            _logApplicationMock;

        private readonly Mock<ILogger<SolutionCenterController>>
            _loggerMock;

        private readonly SolutionCenterController
            _controller;

        public SolutionCenterControllerTests()
        {
            _solutionCenterApplicationMock =
                new Mock<ISolutionCenterApplication>();

            _logApplicationMock =
                new Mock<ILogApplication>();

            _loggerMock =
                new Mock<ILogger<SolutionCenterController>>();

            _controller =
                new SolutionCenterController(
                    _solutionCenterApplicationMock.Object,
                    _logApplicationMock.Object,
                    _loggerMock.Object);
        }

        private static SolutionCenterDetailDto
            CreateSolutionCenterDetail(
                long id = 1,
                string code = "BL01",
                string name = "Perecederos")
        {
            return new SolutionCenterDetailDto
            {
                SolutionCenterId = id,
                SolutionCenterCode = code,
                SolutionCenterName = name
            };
        }

        // =========================================================
        // GET TYPES
        // =========================================================

        [Fact]
        public async Task GetSolutionCenterTypes_ShouldReturnOkWithIsSuccessFalse_WhenNoTypesExist()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.GetSolutionCenterTypes())
                .ReturnsAsync(
                    new List<SolutionCenterType>());

            var result =
                await _controller
                    .GetSolutionCenterTypes();

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "No hay tipos de bodegas y puntos de venta registrados.",
                response.Message);
        }

        [Fact]
        public async Task GetSolutionCenterTypes_ShouldReturnOk_WhenTypesExist()
        {
            var types =
                new List<SolutionCenterType>
                {
                    new SolutionCenterType()
                };

            _solutionCenterApplicationMock
                .Setup(x => x.GetSolutionCenterTypes())
                .ReturnsAsync(types);

            var result =
                await _controller
                    .GetSolutionCenterTypes();

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Tipos de bodegas y puntos de venta consultados correctamente.",
                response.Message);
        }

        [Fact]
        public async Task GetSolutionCenterTypes_ShouldReturn500_WhenApplicationThrowsException()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.GetSolutionCenterTypes())
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            var result =
                await _controller
                    .GetSolutionCenterTypes();

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);

            var response =
                Assert.IsType<ResponseApi>(
                    objectResult.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "Ocurrió un error al consultar los tipos de bodegas y puntos de venta.",
                response.Message);
        }

        // =========================================================
        // CREATE SOLUTION CENTER
        // =========================================================

        [Fact]
        public async Task CreateSolutionCenter_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSolutionCenter(
                    null!);

            Assert.IsType<BadRequestObjectResult>(
                result);

            _solutionCenterApplicationMock.Verify(
                x => x.CreateSolutionCenter(
                    It.IsAny<CreateSolutionCenterDto>()),
                Times.Never);
        }

        [Theory]
        [InlineData(0, "BL01", "Perecederos")]
        [InlineData(-1, "BL01", "Perecederos")]
        [InlineData(1, "", "Perecederos")]
        [InlineData(1, " ", "Perecederos")]
        [InlineData(1, "BL01", "")]
        [InlineData(1, "BL01", " ")]
        public async Task CreateSolutionCenter_ShouldReturnBadRequest_WhenRequestIsInvalid(
            long typeId,
            string code,
            string name)
        {
            var request =
                new CreateSolutionCenterDto
                {
                    SolutionCenterTypeId =
                        typeId,

                    SolutionCenterCode =
                        code,

                    SolutionCenterName =
                        name
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSolutionCenter(
                    request);

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "La información de la bodega no es válida.",
                response.Message);

            _solutionCenterApplicationMock.Verify(
                x => x.CreateSolutionCenter(
                    It.IsAny<CreateSolutionCenterDto>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateSolutionCenter_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new CreateSolutionCenterDto
                {
                    SolutionCenterTypeId = 1,
                    SolutionCenterCode = "BL01",
                    SolutionCenterName =
                        "Perecederos"
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .CreateSolutionCenter(
                    request);

            var forbidden =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);

            var response =
                Assert.IsType<ResponseApi>(
                    forbidden.Value);

            Assert.Equal(
                "El token no contiene un userLogin válido.",
                response.Message);

            _solutionCenterApplicationMock.Verify(
                x => x.CreateSolutionCenter(
                    It.IsAny<CreateSolutionCenterDto>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateSolutionCenter_ShouldReturnBadRequest_WhenApplicationReturnsZero()
        {
            var request =
                new CreateSolutionCenterDto
                {
                    SolutionCenterTypeId = 1,
                    SolutionCenterCode = "BL01",
                    SolutionCenterName =
                        "Perecederos"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.CreateSolutionCenter(
                        request))
                .ReturnsAsync(0);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSolutionCenter(
                    request);

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateSolutionCenter_ShouldReturnOk_WhenCreated()
        {
            var request =
                new CreateSolutionCenterDto
                {
                    SolutionCenterTypeId = 1,
                    SolutionCenterCode =
                        "  bl01  ",
                    SolutionCenterName =
                        "  Perecederos  "
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.CreateSolutionCenter(
                        request))
                .ReturnsAsync(5);

            var result =
                await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                    .CreateSolutionCenter(
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Bodega creada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Crear" &&
                        log.Module ==
                            "ConfiguracionBodegas" &&
                        log.Description ==
                            "Se creó la bodega Perecederos con código BL01." &&
                        log.UserName ==
                            "juan.zapata")),
                Times.Once);
        }

        [Fact]
        public async Task CreateSolutionCenter_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new CreateSolutionCenterDto
                {
                    SolutionCenterTypeId = 1,
                    SolutionCenterCode = "BL01",
                    SolutionCenterName =
                        "Perecederos"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.CreateSolutionCenter(
                        request))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSolutionCenter(
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // CREATE SECTION CONFIGURATION
        // =========================================================

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CreateSectionConfiguration_ShouldReturnBadRequest_WhenSolutionCenterIdIsInvalid(
            long solutionCenterId)
        {
            var request =
                new CreateSectionConfigurationDto
                {
                    SectionName =
                        "Linea de sal"
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSectionConfiguration(
                    solutionCenterId,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);

            _solutionCenterApplicationMock.Verify(
                x => x.CreateSectionConfiguration(
                    It.IsAny<long>(),
                    It.IsAny<CreateSectionConfigurationDto>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateSectionConfiguration_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSectionConfiguration(
                    1,
                    null!);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task CreateSectionConfiguration_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new CreateSectionConfigurationDto
                {
                    SectionName =
                        "Linea de sal"
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .CreateSectionConfiguration(
                    1,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);

            _solutionCenterApplicationMock.Verify(
                x => x.CreateSectionConfiguration(
                    It.IsAny<long>(),
                    It.IsAny<CreateSectionConfigurationDto>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateSectionConfiguration_ShouldReturnBadRequest_WhenApplicationReturnsZero()
        {
            var request =
                new CreateSectionConfigurationDto
                {
                    SectionName =
                        "Linea de sal"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.CreateSectionConfiguration(
                        1,
                        request,
                        "juan.zapata"))
                .ReturnsAsync(0);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSectionConfiguration(
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateSectionConfiguration_ShouldReturnOk_WhenCreated()
        {
            var request =
                new CreateSectionConfigurationDto
                {
                    SectionName =
                        "  Linea de sal  "
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.CreateSectionConfiguration(
                        1,
                        request,
                        "juan.zapata"))
                .ReturnsAsync(2);

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ReturnsAsync(
                    CreateSolutionCenterDetail(
                        1,
                        "BL01",
                        "Perecederos"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSectionConfiguration(
                    1,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Configuración de la sección guardada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Crear" &&
                        log.Module ==
                            "ConfiguracionBodegas" &&
                        log.Description ==
                            "Se creó la sección Linea de sal en la bodega Perecederos.")),
                Times.Once);
        }

        [Fact]
        public async Task CreateSectionConfiguration_ShouldUseIdInLog_WhenSolutionCenterDetailIsNull()
        {
            var request =
                new CreateSectionConfigurationDto
                {
                    SectionName =
                        "Linea de sal"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.CreateSectionConfiguration(
                        5,
                        request,
                        "juan.zapata"))
                .ReturnsAsync(2);

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(5))
                .ReturnsAsync(
                    (SolutionCenterDetailDto?)null);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSectionConfiguration(
                    5,
                    request);

            Assert.IsType<OkObjectResult>(
                result);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Description.Contains(
                            "en la bodega 5."))),
                Times.Once);
        }

        [Fact]
        public async Task CreateSectionConfiguration_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new CreateSectionConfigurationDto
                {
                    SectionName =
                        "Linea de sal"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.CreateSectionConfiguration(
                        1,
                        request,
                        "juan.zapata"))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .CreateSectionConfiguration(
                    1,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // ASSIGN EXISTING SECTION
        // =========================================================

        [Fact]
        public async Task AssignExistingSection_ShouldReturnBadRequest_WhenSolutionCenterIdIsInvalid()
        {
            var result = await _controller.WithIdentity()
                .AssignExistingSection(0, 2);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(badRequest.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal("Los identificadores deben ser mayores a cero.", response.Message);

            _solutionCenterApplicationMock.Verify(
                x => x.AssignExistingSection(It.IsAny<long>(), It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturnBadRequest_WhenSectionIdIsInvalid()
        {
            var result = await _controller.WithIdentity()
                .AssignExistingSection(1, 0);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(badRequest.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal("Los identificadores deben ser mayores a cero.", response.Message);

            _solutionCenterApplicationMock.Verify(
                x => x.AssignExistingSection(It.IsAny<long>(), It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturnForbidden_WhenUserLoginClaimIsMissing()
        {
            var result = await _controller.WithIdentity(userLogin: null)
                .AssignExistingSection(1, 2);

            var forbidden = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(forbidden.Value);

            Assert.Equal(403, forbidden.StatusCode);
            Assert.False(response.IsSuccess);
            Assert.Equal("El token no contiene un userLogin válido.", response.Message);

            _solutionCenterApplicationMock.Verify(
                x => x.AssignExistingSection(It.IsAny<long>(), It.IsAny<long>()),
                Times.Never);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturnNotFound_WhenSolutionCenterDoesNotExist()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.AssignExistingSection(1, 2))
                .ReturnsAsync(new SolutionCenterSectionResultDto
                {
                    Status = SolutionCenterSectionStatus.SolutionCenterNotFound
                });

            var result = await _controller.WithIdentity()
                .AssignExistingSection(1, 2);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(notFound.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal("La bodega o punto de venta no existe.", response.Message);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturnNotFound_WhenSectionDoesNotExist()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.AssignExistingSection(1, 2))
                .ReturnsAsync(new SolutionCenterSectionResultDto
                {
                    Status = SolutionCenterSectionStatus.SectionNotFound
                });

            var result = await _controller.WithIdentity()
                .AssignExistingSection(1, 2);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(notFound.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal("La sección no existe.", response.Message);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturnBadRequest_WhenGlobalSectionIsInactive()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.AssignExistingSection(1, 2))
                .ReturnsAsync(new SolutionCenterSectionResultDto
                {
                    Status = SolutionCenterSectionStatus.SectionInactive
                });

            var result = await _controller.WithIdentity()
                .AssignExistingSection(1, 2);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(badRequest.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal(
                "La sección está inactiva en el catálogo. Actívela antes de asignarla o reactivar su asignación.",
                response.Message);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturnBadRequest_WhenAlreadyAssigned()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.AssignExistingSection(1, 2))
                .ReturnsAsync(new SolutionCenterSectionResultDto
                {
                    Status = SolutionCenterSectionStatus.AlreadyAssigned
                });

            var result = await _controller.WithIdentity()
                .AssignExistingSection(1, 2);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(badRequest.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal("La sección ya está asignada a la bodega o punto de venta.", response.Message);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturnOk_WhenInactiveAssignmentIsReactivated()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.AssignExistingSection(1, 2))
                .ReturnsAsync(new SolutionCenterSectionResultDto
                {
                    Status = SolutionCenterSectionStatus.Success,
                    SolutionCenterName = "No perecederos",
                    SectionName = "Pasillo 1",
                    WasReactivated = true
                });

            var result = await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                .AssignExistingSection(1, 2);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);
            Assert.Equal("Sección asignada correctamente.", response.Message);
            _logApplicationMock.Verify(
                x => x.CreateLog(It.Is<CreateLogDto>(log =>
                    log.Action == "Crear" &&
                    log.Module == "ConfiguracionBodegas" &&
                    log.UserName == "juan.zapata" &&
                    log.Description == "Se reactivó la sección Pasillo 1 en la bodega o punto de venta No perecederos.")),
                Times.Once);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturnOk_WhenNewAssignmentIsCreated()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.AssignExistingSection(1, 2))
                .ReturnsAsync(new SolutionCenterSectionResultDto
                {
                    Status = SolutionCenterSectionStatus.Success,
                    SolutionCenterName = "No perecederos",
                    SectionName = "Pasillo 1"
                });

            var result = await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                .AssignExistingSection(1, 2);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);
            Assert.Equal("Sección asignada correctamente.", response.Message);
            _logApplicationMock.Verify(
                x => x.CreateLog(It.Is<CreateLogDto>(log =>
                    log.Action == "Crear" &&
                    log.Module == "ConfiguracionBodegas" &&
                    log.UserName == "juan.zapata" &&
                    log.Description == "Se asignó la sección Pasillo 1 a la bodega o punto de venta No perecederos.")),
                Times.Once);
            _solutionCenterApplicationMock.Verify(
                x => x.CreateSectionConfiguration(
                    It.IsAny<long>(), It.IsAny<CreateSectionConfigurationDto>(), It.IsAny<string>()),
                Times.Never);
            _solutionCenterApplicationMock.Verify(
                x => x.AddProductToSection(
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<AddSectionProductDto>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task AssignExistingSection_ShouldReturn500_WhenApplicationThrowsException()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.AssignExistingSection(1, 2))
                .ThrowsAsync(new Exception("Error de prueba"));

            var result = await _controller.WithIdentity()
                .AssignExistingSection(1, 2);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);

            Assert.Equal(500, objectResult.StatusCode);
            Assert.False(response.IsSuccess);
            Assert.Equal("Ocurrió un error al asignar la sección.", response.Message);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        // =========================================================
        // UPDATE SECTION ASSIGNMENT STATUS
        // =========================================================

        [Fact]
        public async Task UpdateSectionAssignmentStatus_ShouldReturnOk_WhenUpdated()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.UpdateSectionAssignmentStatus(1, 2, false))
                .ReturnsAsync(new SolutionCenterSectionResultDto
                {
                    Status = SolutionCenterSectionStatus.Success,
                    SolutionCenterName = "No perecederos",
                    SectionName = "Pasillo 1"
                });

            var result = await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                .UpdateSectionAssignmentStatus(1, 2, new UpdateStatusDto { IsActive = false });

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(ok.Value);

            Assert.True(response.IsSuccess);
            Assert.Equal("Estado de la asignación actualizado correctamente.", response.Message);
            _solutionCenterApplicationMock.Verify(x => x.UpdateSectionAssignmentStatus(1, 2, false), Times.Once);
            _solutionCenterApplicationMock.Verify(
                x => x.UpdateSectionStatus(It.IsAny<long>(), It.IsAny<bool>()), Times.Never);
            _logApplicationMock.Verify(
                x => x.CreateLog(It.Is<CreateLogDto>(log =>
                    log.Action == "Actualizar" &&
                    log.Module == "ConfiguracionBodegas" &&
                    log.UserName == "juan.zapata" &&
                    log.Description == "Se inactivó la asignación de la sección Pasillo 1 en la bodega o punto de venta No perecederos.")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSectionAssignmentStatus_ShouldReturnForbidden_WhenUserLoginClaimIsMissing()
        {
            var result = await _controller.WithIdentity(userLogin: null)
                .UpdateSectionAssignmentStatus(1, 2, new UpdateStatusDto { IsActive = false });

            var forbidden = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(forbidden.Value);

            Assert.Equal(403, forbidden.StatusCode);
            Assert.False(response.IsSuccess);
            Assert.Equal("El token no contiene un userLogin válido.", response.Message);
            _solutionCenterApplicationMock.Verify(
                x => x.UpdateSectionAssignmentStatus(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Never);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSectionAssignmentStatus_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.UpdateSectionAssignmentStatus(1, 2, false))
                .ReturnsAsync(new SolutionCenterSectionResultDto
                {
                    Status = SolutionCenterSectionStatus.AssignmentNotFound
                });

            var result = await _controller.WithIdentity()
                .UpdateSectionAssignmentStatus(1, 2, new UpdateStatusDto { IsActive = false });

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(notFound.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal("La sección no está asignada a la bodega o punto de venta.", response.Message);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSectionAssignmentStatus_ShouldReturn500_WhenApplicationThrowsException()
        {
            _solutionCenterApplicationMock
                .Setup(x => x.UpdateSectionAssignmentStatus(1, 2, false))
                .ThrowsAsync(new Exception("Error de prueba"));

            var result = await _controller.WithIdentity()
                .UpdateSectionAssignmentStatus(1, 2, new UpdateStatusDto { IsActive = false });

            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseApi>(objectResult.Value);

            Assert.Equal(500, objectResult.StatusCode);
            Assert.False(response.IsSuccess);
            Assert.Equal("Ocurrió un error al actualizar el estado de la asignación.", response.Message);
            _logApplicationMock.Verify(x => x.CreateLog(It.IsAny<CreateLogDto>()), Times.Never);
        }

        // =========================================================
        // GET SOLUTION CENTERS
        // =========================================================

        [Fact]
        public async Task GetSolutionCenters_ShouldReturnForbidden_WhenRoleClaimIsEmpty()
        {
            var result =
                await _controller.WithIdentity(role: "")
                    .GetSolutionCenters(
                    1,
                    10);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);

            _solutionCenterApplicationMock.Verify(
                x => x.GetPagedSolutionCenters(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public async Task GetSolutionCenters_ShouldReturnBadRequest_WhenPaginationIsInvalid(
            int page,
            int take)
        {
            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetSolutionCenters(
                    page,
                    take);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task GetSolutionCenters_ShouldReturnForbidden_WhenRoleIsInvalid()
        {
            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetPagedSolutionCenters(
                        "INVALIDO",
                        1,
                        10))
                .ReturnsAsync(
                    (PagedDto<SolutionCenterListDto>?)null);

            var result =
                await _controller.WithIdentity(role: "INVALIDO")
                    .GetSolutionCenters(
                    1,
                    10);

            var forbidden =
                Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);

            var response =
                Assert.IsType<ResponseApi>(
                    forbidden.Value);

            Assert.Equal(
                "El rol del token no tiene permisos para esta consulta.",
                response.Message);
        }

        [Fact]
        public async Task GetSolutionCenters_ShouldReturnOkWithIsSuccessFalse_WhenNoRecordsExist()
        {
            var paged =
                new PagedDto<SolutionCenterListDto>
                {
                    Items =
                        new List<SolutionCenterListDto>(),

                    Total = 0,
                    Page = 1,
                    Take = 10,
                    Pages = 0
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetPagedSolutionCenters(
                        "COSTOS",
                        1,
                        10))
                .ReturnsAsync(paged);

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetSolutionCenters(
                    1,
                    10);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "No hay bodegas o puntos de venta disponibles para el rol.",
                response.Message);
        }

        [Fact]
        public async Task GetSolutionCenters_ShouldReturnOk_WhenRecordsExist()
        {
            var paged =
                new PagedDto<SolutionCenterListDto>
                {
                    Items =
                        new List<SolutionCenterListDto>
                        {
                            new SolutionCenterListDto()
                        },

                    Total = 1,
                    Page = 1,
                    Take = 10,
                    Pages = 1
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetPagedSolutionCenters(
                        "COSTOS",
                        1,
                        10))
                .ReturnsAsync(paged);

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetSolutionCenters(
                    1,
                    10);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Same(
                paged,
                response.Result);
        }

        [Fact]
        public async Task GetSolutionCenters_ShouldReturn500_WhenApplicationThrowsException()
        {
            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetPagedSolutionCenters(
                        "COSTOS",
                        1,
                        10))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            var result =
                await _controller.WithIdentity(role: "COSTOS")
                    .GetSolutionCenters(
                    1,
                    10);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // GET SOLUTION CENTER BY ID
        // =========================================================

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetSolutionCenterById_ShouldReturnBadRequest_WhenIdIsInvalid(
            long id)
        {
            var result =
                await _controller
                    .GetSolutionCenterById(id);

            Assert.IsType<BadRequestObjectResult>(
                result);

            _solutionCenterApplicationMock.Verify(
                x => x.GetSolutionCenterById(
                    It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task GetSolutionCenterById_ShouldReturnNotFound_WhenDoesNotExist()
        {
            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(999))
                .ReturnsAsync(
                    (SolutionCenterDetailDto?)null);

            var result =
                await _controller
                    .GetSolutionCenterById(999);

            Assert.IsType<NotFoundObjectResult>(
                result);
        }

        [Fact]
        public async Task GetSolutionCenterById_ShouldReturnOk_WhenExists()
        {
            var detail =
                CreateSolutionCenterDetail();

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ReturnsAsync(detail);

            var result =
                await _controller
                    .GetSolutionCenterById(1);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Same(
                detail,
                response.Result);
        }

        [Fact]
        public async Task GetSolutionCenterById_ShouldReturn500_WhenApplicationThrowsException()
        {
            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            var result =
                await _controller
                    .GetSolutionCenterById(1);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // UPDATE SOLUTION CENTER STATUS
        // =========================================================

        [Fact]
        public async Task UpdateSolutionCenterStatus_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenterStatus(
                    0,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSolutionCenterStatus_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenterStatus(
                    1,
                    null!);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSolutionCenterStatus_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .UpdateSolutionCenterStatus(
                    1,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task UpdateSolutionCenterStatus_ShouldReturnNotFound_WhenSolutionCenterDoesNotExist()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(999))
                .ReturnsAsync(
                    (SolutionCenterDetailDto?)null);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenterStatus(
                    999,
                    request);

            Assert.IsType<NotFoundObjectResult>(
                result);

            _solutionCenterApplicationMock.Verify(
                x => x.UpdateSolutionCenterStatus(
                    It.IsAny<long>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateSolutionCenterStatus_ShouldReturnNotFound_WhenUpdateReturnsFalse()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ReturnsAsync(
                    CreateSolutionCenterDetail());

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateSolutionCenterStatus(
                        1,
                        true))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenterStatus(
                    1,
                    request);

            Assert.IsType<NotFoundObjectResult>(
                result);
        }

        [Theory]
        [InlineData(true, "Bodega activada correctamente.")]
        [InlineData(false, "Bodega inactivada correctamente.")]
        public async Task UpdateSolutionCenterStatus_ShouldReturnOk_WhenUpdated(
            bool isActive,
            string expectedMessage)
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive =
                        isActive
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ReturnsAsync(
                    CreateSolutionCenterDetail());

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateSolutionCenterStatus(
                        1,
                        isActive))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                    .UpdateSolutionCenterStatus(
                    1,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                expectedMessage,
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Actualizar" &&
                        log.Module ==
                            "ConfiguracionBodegas" &&
                        log.UserName ==
                            "juan.zapata")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSolutionCenterStatus_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ThrowsAsync(
                    new Exception(
                        "Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenterStatus(
                    1,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // UPDATE SECTION STATUS
        // =========================================================

        [Fact]
        public async Task UpdateSectionStatus_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSectionStatus(
                    0,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSectionStatus_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSectionStatus(
                    2,
                    null!);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSectionStatus_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .UpdateSectionStatus(
                    2,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task UpdateSectionStatus_ShouldReturnNotFound_WhenApplicationReturnsFalse()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = false
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateSectionStatus(
                        2,
                        false))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSectionStatus(
                    2,
                    request);

            Assert.IsType<NotFoundObjectResult>(
                result);
        }

        [Theory]
        [InlineData(true, "Sección activada correctamente.")]
        [InlineData(false, "Sección inactivada correctamente.")]
        public async Task UpdateSectionStatus_ShouldReturnOk_WhenUpdated(
            bool isActive,
            string expectedMessage)
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive =
                        isActive
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateSectionStatus(
                        2,
                        isActive))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSectionStatus(
                    2,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                expectedMessage,
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action ==
                            "Actualizar")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSectionStatus_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateSectionStatus(
                        2,
                        true))
                .ThrowsAsync(
                    new Exception(
                        "Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSectionStatus(
                    2,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // ADD PRODUCT TO SECTION
        // =========================================================

        [Fact]
        public async Task AddProductToSection_ShouldReturnBadRequest_WhenSolutionCenterIdIsInvalid()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 10,
                    Position = 1
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddProductToSection(
                    0,
                    2,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnBadRequest_WhenSectionIdIsInvalid()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 10,
                    Position = 1
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddProductToSection(
                    1,
                    0,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddProductToSection(
                    1,
                    2,
                    null!);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnBadRequest_WhenProductIdIsInvalid()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 0,
                    Position = 1
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddProductToSection(
                    1,
                    2,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnBadRequest_WhenPositionIsInvalid()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 10,
                    Position = 0
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddProductToSection(
                    1,
                    2,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 10,
                    Position = 1
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .AddProductToSection(
                    1,
                    2,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnBadRequest_WhenApplicationReturnsZero()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 10,
                    Position = 1
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.AddProductToSection(
                        1,
                        2,
                        request, "juan.zapata"))
                .ReturnsAsync(0);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddProductToSection(
                    1,
                    2,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnOk_WhenProductIsAdded()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 10,
                    Position = 3
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.AddProductToSection(
                        1,
                        2,
                        request, "juan.zapata"))
                .ReturnsAsync(100);

            var result =
                await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                    .AddProductToSection(
                    1,
                    2,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            _solutionCenterApplicationMock.Verify(
                x => x.AddProductToSection(1, 2, request, "juan.zapata"),
                Times.Once);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Crear" &&
                        log.Module ==
                            "ConfiguracionBodegas" &&
                        log.UserName ==
                            "juan.zapata" &&
                        log.Description.Contains(
                            "ID 10") &&
                        log.Description.Contains(
                            "posición 3"))),
                Times.Once);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 10,
                    Position = 1
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.AddProductToSection(
                        1,
                        2,
                        request, "juan.zapata"))
                .ThrowsAsync(
                    new Exception(
                        "Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .AddProductToSection(
                    1,
                    2,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // UPDATE PRODUCT ORDER
        // =========================================================

        [Fact]
        public async Task UpdateProductOrder_ShouldReturnBadRequest_WhenIdsAreInvalid()
        {
            var request =
                new UpdateProductOrderDto
                {
                    NewPosition = 1
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateProductOrder(
                    0,
                    2,
                    3,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateProductOrder_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateProductOrder(
                    1,
                    2,
                    3,
                    null!);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateProductOrder_ShouldReturnBadRequest_WhenNewPositionIsInvalid()
        {
            var request =
                new UpdateProductOrderDto
                {
                    NewPosition = 0
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateProductOrder(
                    1,
                    2,
                    3,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateProductOrder_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new UpdateProductOrderDto
                {
                    NewPosition = 2
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .UpdateProductOrder(
                    1,
                    2,
                    3,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task UpdateProductOrder_ShouldReturnBadRequest_WhenApplicationReturnsFalse()
        {
            var request =
                new UpdateProductOrderDto
                {
                    NewPosition = 2
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateProductOrder(
                        1,
                        2,
                        3,
                        2))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateProductOrder(
                    1,
                    2,
                    3,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateProductOrder_ShouldReturnOk_WhenUpdated()
        {
            var request =
                new UpdateProductOrderDto
                {
                    NewPosition = 2
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateProductOrder(
                        1,
                        2,
                        3,
                        2))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateProductOrder(
                    1,
                    2,
                    3,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Orden del producto actualizado correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action ==
                            "Actualizar" &&
                        log.Description.Contains(
                            "posición 2"))),
                Times.Once);
        }

        [Fact]
        public async Task UpdateProductOrder_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new UpdateProductOrderDto
                {
                    NewPosition = 2
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateProductOrder(
                        1,
                        2,
                        3,
                        2))
                .ThrowsAsync(
                    new Exception(
                        "Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateProductOrder(
                    1,
                    2,
                    3,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // DELETE PRODUCT FROM SECTION
        // =========================================================

        [Fact]
        public async Task DeleteProductFromSection_ShouldReturnBadRequest_WhenIdsAreInvalid()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteProductFromSection(
                    0,
                    2,
                    3);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task DeleteProductFromSection_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var result =
                await _controller.WithIdentity(userLogin: "")
                    .DeleteProductFromSection(
                    1,
                    2,
                    3);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task DeleteProductFromSection_ShouldReturnBadRequest_WhenApplicationReturnsFalse()
        {
            _solutionCenterApplicationMock
                .Setup(x =>
                    x.DeleteProductFromSection(
                        1,
                        2,
                        3))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteProductFromSection(
                    1,
                    2,
                    3);

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "No se pudo eliminar el producto de la sección. " +
                "Verifique que el producto pertenezca a una asignación activa del centro y la sección.",
                response.Message);
        }

        [Fact]
        public async Task DeleteProductFromSection_ShouldReturnOk_WhenDeleted()
        {
            _solutionCenterApplicationMock
                .Setup(x =>
                    x.DeleteProductFromSection(
                        1,
                        2,
                        3))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                    .DeleteProductFromSection(
                    1,
                    2,
                    3);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Producto eliminado de la sección correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action ==
                            "Eliminar" &&
                        log.Module ==
                            "ConfiguracionBodegas" &&
                        log.UserName ==
                            "juan.zapata" &&
                        log.Description.Contains(
                            "ID 3"))),
                Times.Once);
        }

        [Fact]
        public async Task DeleteProductFromSection_ShouldReturn500_WhenApplicationThrowsException()
        {
            _solutionCenterApplicationMock
                .Setup(x =>
                    x.DeleteProductFromSection(
                        1,
                        2,
                        3))
                .ThrowsAsync(
                    new Exception(
                        "Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .DeleteProductFromSection(
                    1,
                    2,
                    3);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // UPDATE SOLUTION CENTER
        // =========================================================

        [Fact]
        public async Task UpdateSolutionCenter_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode = "BL01",
                    SolutionCenterName =
                        "Perecederos"
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenter(
                    0,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSolutionCenter_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenter(
                    1,
                    null!);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Theory]
        [InlineData("", "Perecederos")]
        [InlineData(" ", "Perecederos")]
        [InlineData("BL01", "")]
        [InlineData("BL01", " ")]
        public async Task UpdateSolutionCenter_ShouldReturnBadRequest_WhenCodeOrNameIsEmpty(
            string code,
            string name)
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode =
                        code,

                    SolutionCenterName =
                        name
                };

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenter(
                    1,
                    request);

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.Equal(
                "El código y el nombre son obligatorios.",
                response.Message);
        }

        [Fact]
        public async Task UpdateSolutionCenter_ShouldReturnForbidden_WhenUserLoginClaimIsEmpty()
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode = "BL01",
                    SolutionCenterName =
                        "Perecederos"
                };

            var result =
                await _controller.WithIdentity(userLogin: "")
                    .UpdateSolutionCenter(
                    1,
                    request);

            Assert.IsType<ObjectResult>(result);

            Assert.Equal(403, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task UpdateSolutionCenter_ShouldReturnNotFound_WhenDoesNotExist()
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode = "BL01",
                    SolutionCenterName =
                        "Perecederos"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(999))
                .ReturnsAsync(
                    (SolutionCenterDetailDto?)null);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenter(
                    999,
                    request);

            Assert.IsType<NotFoundObjectResult>(
                result);

            _solutionCenterApplicationMock.Verify(
                x => x.UpdateSolutionCenter(
                    It.IsAny<long>(),
                    It.IsAny<UpdateSolutionCenterDto>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateSolutionCenter_ShouldReturnBadRequest_WhenApplicationReturnsFalse()
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode = "BL02",
                    SolutionCenterName =
                        "Perecederos 2"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ReturnsAsync(
                    CreateSolutionCenterDetail());

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateSolutionCenter(
                        1,
                        request))
                .ReturnsAsync(false);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenter(
                    1,
                    request);

            Assert.IsType<BadRequestObjectResult>(
                result);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateSolutionCenter_ShouldReturnOk_WhenCodeAndNameChange()
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode =
                        "  bl02  ",

                    SolutionCenterName =
                        "  Congelados  "
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ReturnsAsync(
                    CreateSolutionCenterDetail(
                        1,
                        "BL01",
                        "Perecederos"));

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateSolutionCenter(
                        1,
                        request))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "  juan.zapata  ")
                    .UpdateSolutionCenter(
                    1,
                    request);

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Bodega actualizada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action ==
                            "Actualizar" &&
                        log.Module ==
                            "ConfiguracionBodegas" &&
                        log.UserName ==
                            "juan.zapata" &&
                        log.Description.Contains(
                            "código de BL01 a BL02") &&
                        log.Description.Contains(
                            "nombre de Perecederos a Congelados"))),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSolutionCenter_ShouldLogNoChanges_WhenValuesAreTheSame()
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode =
                        "bl01",

                    SolutionCenterName =
                        "Perecederos"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ReturnsAsync(
                    CreateSolutionCenterDetail());

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.UpdateSolutionCenter(
                        1,
                        request))
                .ReturnsAsync(true);

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenter(
                    1,
                    request);

            Assert.IsType<OkObjectResult>(
                result);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Description.Contains(
                            "sin cambios en nombre o código"))),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSolutionCenter_ShouldReturn500_WhenApplicationThrowsException()
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode = "BL02",
                    SolutionCenterName =
                        "Congelados"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.GetSolutionCenterById(1))
                .ThrowsAsync(
                    new Exception(
                        "Error"));

            var result =
                await _controller.WithIdentity(userLogin: "juan.zapata")
                    .UpdateSolutionCenter(
                    1,
                    request);

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }
    }
}
