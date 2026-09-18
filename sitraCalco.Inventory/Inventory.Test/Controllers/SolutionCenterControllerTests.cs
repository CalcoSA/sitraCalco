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
                await _controller
                    .CreateSolutionCenter(
                        null!,
                        "juan.zapata");

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
                await _controller
                    .CreateSolutionCenter(
                        request,
                        "juan.zapata");

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
        public async Task CreateSolutionCenter_ShouldReturnBadRequest_WhenUserIsEmpty()
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
                await _controller
                    .CreateSolutionCenter(
                        request,
                        "");

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.Equal(
                "El usuario que ejecuta la operación es obligatorio.",
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
                await _controller
                    .CreateSolutionCenter(
                        request,
                        "juan.zapata");

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
                await _controller
                    .CreateSolutionCenter(
                        request,
                        "  juan.zapata  ");

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
                await _controller
                    .CreateSolutionCenter(
                        request,
                        "juan.zapata");

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
                await _controller
                    .CreateSectionConfiguration(
                        solutionCenterId,
                        request,
                        "juan.zapata");

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
                await _controller
                    .CreateSectionConfiguration(
                        1,
                        null!,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task CreateSectionConfiguration_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new CreateSectionConfigurationDto
                {
                    SectionName =
                        "Linea de sal"
                };

            var result =
                await _controller
                    .CreateSectionConfiguration(
                        1,
                        request,
                        "");

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
                await _controller
                    .CreateSectionConfiguration(
                        1,
                        request,
                        "juan.zapata");

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
                await _controller
                    .CreateSectionConfiguration(
                        1,
                        request,
                        "juan.zapata");

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
                await _controller
                    .CreateSectionConfiguration(
                        5,
                        request,
                        "juan.zapata");

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
                await _controller
                    .CreateSectionConfiguration(
                        1,
                        request,
                        "juan.zapata");

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }

        // =========================================================
        // GET SOLUTION CENTERS
        // =========================================================

        [Fact]
        public async Task GetSolutionCenters_ShouldReturnBadRequest_WhenRoleIsEmpty()
        {
            var result =
                await _controller
                    .GetSolutionCenters(
                        "",
                        1,
                        10);

            Assert.IsType<BadRequestObjectResult>(
                result);

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
                await _controller
                    .GetSolutionCenters(
                        "COSTOS",
                        page,
                        take);

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task GetSolutionCenters_ShouldReturnBadRequest_WhenRoleIsInvalid()
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
                await _controller
                    .GetSolutionCenters(
                        "INVALIDO",
                        1,
                        10);

            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.Equal(
                "El rol enviado no es válido.",
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
                await _controller
                    .GetSolutionCenters(
                        "COSTOS",
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
                await _controller
                    .GetSolutionCenters(
                        "COSTOS",
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
                await _controller
                    .GetSolutionCenters(
                        "COSTOS",
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
                await _controller
                    .UpdateSolutionCenterStatus(
                        0,
                        request,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSolutionCenterStatus_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller
                    .UpdateSolutionCenterStatus(
                        1,
                        null!,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSolutionCenterStatus_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller
                    .UpdateSolutionCenterStatus(
                        1,
                        request,
                        "");

            Assert.IsType<BadRequestObjectResult>(
                result);
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
                await _controller
                    .UpdateSolutionCenterStatus(
                        999,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateSolutionCenterStatus(
                        1,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateSolutionCenterStatus(
                        1,
                        request,
                        "  juan.zapata  ");

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
                await _controller
                    .UpdateSolutionCenterStatus(
                        1,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateSectionStatus(
                        0,
                        request,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSectionStatus_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller
                    .UpdateSectionStatus(
                        2,
                        null!,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSectionStatus_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new UpdateStatusDto
                {
                    IsActive = true
                };

            var result =
                await _controller
                    .UpdateSectionStatus(
                        2,
                        request,
                        "");

            Assert.IsType<BadRequestObjectResult>(
                result);
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
                await _controller
                    .UpdateSectionStatus(
                        2,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateSectionStatus(
                        2,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateSectionStatus(
                        2,
                        request,
                        "juan.zapata");

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
                await _controller
                    .AddProductToSection(
                        0,
                        2,
                        request,
                        "juan.zapata");

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
                await _controller
                    .AddProductToSection(
                        1,
                        0,
                        request,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller
                    .AddProductToSection(
                        1,
                        2,
                        null!,
                        "juan.zapata");

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
                await _controller
                    .AddProductToSection(
                        1,
                        2,
                        request,
                        "juan.zapata");

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
                await _controller
                    .AddProductToSection(
                        1,
                        2,
                        request,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task AddProductToSection_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new AddSectionProductDto
                {
                    ProductId = 10,
                    Position = 1
                };

            var result =
                await _controller
                    .AddProductToSection(
                        1,
                        2,
                        request,
                        "");

            Assert.IsType<BadRequestObjectResult>(
                result);
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
                        request))
                .ReturnsAsync(0);

            var result =
                await _controller
                    .AddProductToSection(
                        1,
                        2,
                        request,
                        "juan.zapata");

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
                    Position = 3,
                    CreatedBy =
                        "fake-user"
                };

            _solutionCenterApplicationMock
                .Setup(x =>
                    x.AddProductToSection(
                        1,
                        2,
                        request))
                .ReturnsAsync(100);

            var result =
                await _controller
                    .AddProductToSection(
                        1,
                        2,
                        request,
                        "  juan.zapata  ");

            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "juan.zapata",
                request.CreatedBy);

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
                        request))
                .ThrowsAsync(
                    new Exception(
                        "Error"));

            var result =
                await _controller
                    .AddProductToSection(
                        1,
                        2,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateProductOrder(
                        0,
                        2,
                        3,
                        request,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateProductOrder_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller
                    .UpdateProductOrder(
                        1,
                        2,
                        3,
                        null!,
                        "juan.zapata");

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
                await _controller
                    .UpdateProductOrder(
                        1,
                        2,
                        3,
                        request,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateProductOrder_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new UpdateProductOrderDto
                {
                    NewPosition = 2
                };

            var result =
                await _controller
                    .UpdateProductOrder(
                        1,
                        2,
                        3,
                        request,
                        "");

            Assert.IsType<BadRequestObjectResult>(
                result);
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
                await _controller
                    .UpdateProductOrder(
                        1,
                        2,
                        3,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateProductOrder(
                        1,
                        2,
                        3,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateProductOrder(
                        1,
                        2,
                        3,
                        request,
                        "juan.zapata");

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
                await _controller
                    .DeleteProductFromSection(
                        0,
                        2,
                        3,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task DeleteProductFromSection_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var result =
                await _controller
                    .DeleteProductFromSection(
                        1,
                        2,
                        3,
                        "");

            Assert.IsType<BadRequestObjectResult>(
                result);
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
                await _controller
                    .DeleteProductFromSection(
                        1,
                        2,
                        3,
                        "juan.zapata");

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
                "Verifique que la relación exista y que no sea " +
                "el último producto de la sección.",
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
                await _controller
                    .DeleteProductFromSection(
                        1,
                        2,
                        3,
                        "  juan.zapata  ");

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
                await _controller
                    .DeleteProductFromSection(
                        1,
                        2,
                        3,
                        "juan.zapata");

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
                await _controller
                    .UpdateSolutionCenter(
                        0,
                        request,
                        "juan.zapata");

            Assert.IsType<BadRequestObjectResult>(
                result);
        }

        [Fact]
        public async Task UpdateSolutionCenter_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var result =
                await _controller
                    .UpdateSolutionCenter(
                        1,
                        null!,
                        "juan.zapata");

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
                await _controller
                    .UpdateSolutionCenter(
                        1,
                        request,
                        "juan.zapata");

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
        public async Task UpdateSolutionCenter_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            var request =
                new UpdateSolutionCenterDto
                {
                    SolutionCenterCode = "BL01",
                    SolutionCenterName =
                        "Perecederos"
                };

            var result =
                await _controller
                    .UpdateSolutionCenter(
                        1,
                        request,
                        "");

            Assert.IsType<BadRequestObjectResult>(
                result);
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
                await _controller
                    .UpdateSolutionCenter(
                        999,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateSolutionCenter(
                        1,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateSolutionCenter(
                        1,
                        request,
                        "  juan.zapata  ");

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
                await _controller
                    .UpdateSolutionCenter(
                        1,
                        request,
                        "juan.zapata");

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
                await _controller
                    .UpdateSolutionCenter(
                        1,
                        request,
                        "juan.zapata");

            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);
        }
    }
}