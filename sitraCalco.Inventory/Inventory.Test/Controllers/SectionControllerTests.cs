using FluentValidation;
using FluentValidation.Results;
using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections;
using System.Reflection;

namespace Inventory.Test.Controllers
{
    public class SectionControllerTests
    {
        private readonly Mock<ISectionApplication>
            _sectionApplicationMock;

        private readonly Mock<ILogApplication>
            _logApplicationMock;

        private readonly Mock<IValidator<CreateSectionDto>>
            _createSectionValidatorMock;

        private readonly Mock<IValidator<UpdateSectionDto>>
            _updateSectionValidatorMock;

        private readonly Mock<ILogger<SectionController>>
            _loggerMock;

        private readonly SectionController
            _controller;

        public SectionControllerTests()
        {
            _sectionApplicationMock =
                new Mock<ISectionApplication>();

            _logApplicationMock =
                new Mock<ILogApplication>();

            _createSectionValidatorMock =
                new Mock<IValidator<CreateSectionDto>>();

            _updateSectionValidatorMock =
                new Mock<IValidator<UpdateSectionDto>>();

            _loggerMock =
                new Mock<ILogger<SectionController>>();

            ConfigureValidValidators();

            _controller =
                new SectionController(
                    _sectionApplicationMock.Object,
                    _logApplicationMock.Object,
                    _createSectionValidatorMock.Object,
                    _updateSectionValidatorMock.Object,
                    _loggerMock.Object);
        }

        private void ConfigureValidValidators()
        {
            _createSectionValidatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<CreateSectionDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new ValidationResult());

            _updateSectionValidatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<UpdateSectionDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new ValidationResult());
        }

        private static ValidationResult
            CreateInvalidValidationResult(
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
        // GET ALL
        // =========================================================

        [Fact]
        public async Task GetAll_ShouldReturnOkWithIsSuccessFalse_WhenNoSectionsExist()
        {
            // Arrange
            ConfigureGetAllResult(
                includeSection: false);

            // Act
            var result =
                await _controller.GetAll();

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "No hay secciones registradas.",
                response.Message);

            Assert.NotNull(
                response.Result);

            _sectionApplicationMock.Verify(
                x => x.GetAll(),
                Times.Once);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WhenSectionsExist()
        {
            // Arrange
            ConfigureGetAllResult(
                includeSection: true);

            // Act
            var result =
                await _controller.GetAll();

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Secciones consultadas correctamente.",
                response.Message);

            var sections =
                Assert.IsAssignableFrom<IEnumerable>(
                    response.Result);

            Assert.NotEmpty(
                sections.Cast<object>());

            _sectionApplicationMock.Verify(
                x => x.GetAll(),
                Times.Once);
        }

        [Fact]
        public async Task GetAll_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            _sectionApplicationMock
                .Setup(x => x.GetAll())
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.GetAll();

            // Assert
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
                "Ocurrió un error al consultar las secciones.",
                response.Message);
        }

        // =========================================================
        // GET BY ID
        // =========================================================

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetById_ShouldReturnBadRequest_WhenIdIsInvalid(
            long sectionId)
        {
            // Act
            var result =
                await _controller.GetById(
                    sectionId);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "El identificador de la sección debe ser mayor a cero.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.GetById(
                    It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenSectionDoesNotExist()
        {
            // Arrange
            ConfigureGetByIdResult(
                exists: false);

            // Act
            var result =
                await _controller.GetById(
                    999);

            // Assert
            var notFound =
                Assert.IsType<NotFoundObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    notFound.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "La sección no existe.",
                response.Message);
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenSectionExists()
        {
            // Arrange
            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            // Act
            var result =
                await _controller.GetById(
                    2);

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Sección consultada correctamente.",
                response.Message);

            Assert.NotNull(
                response.Result);
        }

        [Fact]
        public async Task GetById_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            _sectionApplicationMock
                .Setup(x =>
                    x.GetById(2))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.GetById(
                    2);

            // Assert
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
                "Ocurrió un error al consultar la sección.",
                response.Message);
        }

        // =========================================================
        // CREATE
        // =========================================================

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            // Arrange
            var request =
                new CreateSectionDto
                {
                    SectionName =
                        "Linea de sal"
                };

            // Act
            var result =
                await _controller.Create(
                    request,
                    "");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "El usuario que ejecuta la operación es obligatorio.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.Create(
                    It.IsAny<CreateSectionDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
        {
            // Arrange
            var request =
                new CreateSectionDto
                {
                    SectionName = ""
                };

            _createSectionValidatorMock
                .Setup(x => x.ValidateAsync(
                    request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateInvalidValidationResult(
                        nameof(
                            CreateSectionDto.SectionName),
                        "El nombre es obligatorio."));

            // Act
            var result =
                await _controller.Create(
                    request,
                    "juan.zapata");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "La solicitud no es válida.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.Create(
                    It.IsAny<CreateSectionDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenApplicationReturnsZero()
        {
            // Arrange
            var request =
                new CreateSectionDto
                {
                    SectionName =
                        "Linea de sal"
                };

            _sectionApplicationMock
                .Setup(x =>
                    x.Create(request))
                .ReturnsAsync(0);

            // Act
            var result =
                await _controller.Create(
                    request,
                    "juan.zapata");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "No se pudo crear la sección. Verifique que el nombre no esté registrado.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_ShouldReturnOk_WhenSectionIsCreated()
        {
            // Arrange
            var request =
                new CreateSectionDto
                {
                    SectionName =
                        "  Linea de sal  "
                };

            _sectionApplicationMock
                .Setup(x =>
                    x.Create(request))
                .ReturnsAsync(2);

            // Act
            var result =
                await _controller.Create(
                    request,
                    "  juan.zapata  ");

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Sección creada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action == "Crear" &&
                        log.Module ==
                            "ConfiguracionSecciones" &&
                        log.Description ==
                            "Se creó la sección Linea de sal." &&
                        log.UserName ==
                            "juan.zapata")),
                Times.Once);
        }

        [Fact]
        public async Task Create_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            var request =
                new CreateSectionDto
                {
                    SectionName =
                        "Linea de sal"
                };

            _sectionApplicationMock
                .Setup(x =>
                    x.Create(request))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.Create(
                    request,
                    "juan.zapata");

            // Assert
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
                "Ocurrió un error al crear la sección.",
                response.Message);
        }

        // =========================================================
        // UPDATE
        // =========================================================

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Update_ShouldReturnBadRequest_WhenIdIsInvalid(
            long sectionId)
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de sal",

                    IsActive =
                        true
                };

            // Act
            var result =
                await _controller.Update(
                    sectionId,
                    request,
                    "juan.zapata");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "El identificador de la sección debe ser mayor a cero.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.Update(
                    It.IsAny<long>(),
                    It.IsAny<UpdateSectionDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de sal",

                    IsActive =
                        true
                };

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "El usuario que ejecuta la operación es obligatorio.",
                response.Message);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenValidationFails()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName = "",
                    IsActive = true
                };

            _updateSectionValidatorMock
                .Setup(x => x.ValidateAsync(
                    request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateInvalidValidationResult(
                        nameof(
                            UpdateSectionDto.SectionName),
                        "El nombre es obligatorio."));

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "juan.zapata");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "La solicitud no es válida.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.GetById(
                    It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenSectionDoesNotExist()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de sal",

                    IsActive =
                        true
                };

            ConfigureGetByIdResult(
                exists: false);

            // Act
            var result =
                await _controller.Update(
                    999,
                    request,
                    "juan.zapata");

            // Assert
            var notFound =
                Assert.IsType<NotFoundObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    notFound.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "La sección no existe.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.Update(
                    It.IsAny<long>(),
                    It.IsAny<UpdateSectionDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenApplicationReturnsFalse()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de dulce",

                    IsActive =
                        true
                };

            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Update(
                        2,
                        request))
                .ReturnsAsync(false);

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "juan.zapata");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "No se pudo actualizar la sección. Verifique que el nombre no esté registrado.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenNameIsChanged()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de dulce",

                    IsActive =
                        true
                };

            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Update(
                        2,
                        request))
                .ReturnsAsync(true);

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "juan.zapata");

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Sección actualizada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action ==
                            "Actualizar" &&
                        log.Module ==
                            "ConfiguracionSecciones" &&
                        log.Description.Contains(
                            "nombre de Linea de sal a Linea de dulce") &&
                        log.UserName ==
                            "juan.zapata")),
                Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenStatusIsChangedToInactive()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de sal",

                    IsActive =
                        false
                };

            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Update(
                        2,
                        request))
                .ReturnsAsync(true);

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "juan.zapata");

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Description.Contains(
                            "estado a inactivo"))),
                Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenNameAndStatusAreChanged()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de dulce",

                    IsActive =
                        false
                };

            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Update(
                        2,
                        request))
                .ReturnsAsync(true);

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "juan.zapata");

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Description.Contains(
                            "nombre de Linea de sal a Linea de dulce") &&
                        log.Description.Contains(
                            "estado a inactivo"))),
                Times.Once);
        }

        [Fact]
        public async Task Update_ShouldLogSinCambios_WhenValuesDoNotChange()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de sal",

                    IsActive =
                        true
                };

            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Update(
                        2,
                        request))
                .ReturnsAsync(true);

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "juan.zapata");

            // Assert
            Assert.IsType<OkObjectResult>(
                result);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Description.Contains(
                            "sin cambios"))),
                Times.Once);
        }

        [Fact]
        public async Task Update_ShouldTrimUserAndSectionName_WhenSuccessful()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "  Linea de sal  ",

                    IsActive =
                        true
                };

            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Update(
                        2,
                        request))
                .ReturnsAsync(true);

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "  juan.zapata  ");

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.UserName ==
                            "juan.zapata" &&
                        log.Description.Contains(
                            "Linea de sal"))),
                Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            var request =
                new UpdateSectionDto
                {
                    SectionName =
                        "Linea de sal",

                    IsActive =
                        true
                };

            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Update(
                        2,
                        request))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.Update(
                    2,
                    request,
                    "juan.zapata");

            // Assert
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
                "Ocurrió un error al actualizar la sección.",
                response.Message);
        }

        // =========================================================
        // DELETE
        // =========================================================

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Delete_ShouldReturnBadRequest_WhenIdIsInvalid(
            long sectionId)
        {
            // Act
            var result =
                await _controller.Delete(
                    sectionId,
                    "juan.zapata");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "El identificador de la sección debe ser mayor a cero.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.Delete(
                    It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task Delete_ShouldReturnBadRequest_WhenUserIsEmpty()
        {
            // Act
            var result =
                await _controller.Delete(
                    2,
                    "");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "El usuario que ejecuta la operación es obligatorio.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.GetById(
                    It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenSectionDoesNotExist()
        {
            // Arrange
            ConfigureGetByIdResult(
                exists: false);

            // Act
            var result =
                await _controller.Delete(
                    999,
                    "juan.zapata");

            // Assert
            var notFound =
                Assert.IsType<NotFoundObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    notFound.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "La sección no existe.",
                response.Message);

            _sectionApplicationMock.Verify(
                x => x.Delete(
                    It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task Delete_ShouldReturnBadRequest_WhenSectionHasAssociatedRecords()
        {
            // Arrange
            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Delete(2))
                .ReturnsAsync(false);

            // Act
            var result =
                await _controller.Delete(
                    2,
                    "juan.zapata");

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "No se puede eliminar la sección porque tiene registros asociados.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.IsAny<CreateLogDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Delete_ShouldReturnOk_WhenSectionIsDeleted()
        {
            // Arrange
            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Delete(2))
                .ReturnsAsync(true);

            // Act
            var result =
                await _controller.Delete(
                    2,
                    "  juan.zapata  ");

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Sección eliminada correctamente.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.CreateLog(
                    It.Is<CreateLogDto>(log =>
                        log.Action ==
                            "Eliminar" &&
                        log.Module ==
                            "ConfiguracionSecciones" &&
                        log.Description ==
                            "Se eliminó la sección Linea de sal." &&
                        log.UserName ==
                            "juan.zapata")),
                Times.Once);
        }

        [Fact]
        public async Task Delete_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            ConfigureGetByIdResult(
                exists: true,
                sectionId: 2,
                sectionName: "Linea de sal",
                isActive: true);

            _sectionApplicationMock
                .Setup(x =>
                    x.Delete(2))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.Delete(
                    2,
                    "juan.zapata");

            // Assert
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
                "Ocurrió un error al eliminar la sección.",
                response.Message);
        }

        // =========================================================
        // HELPERS PARA LOS TIPOS REALES DE SECTION
        // =========================================================

        /// <summary>
        /// Configura GetAll sin depender del nombre exacto
        /// del DTO que retorna ISectionApplication.
        /// </summary>
        private void ConfigureGetAllResult(
            bool includeSection)
        {
            var method =
                typeof(ISectionApplication)
                    .GetMethod(
                        nameof(
                            ISectionApplication.GetAll));

            if (method is null)
            {
                throw new InvalidOperationException(
                    "No se encontró ISectionApplication.GetAll.");
            }

            var taskResultType =
                GetTaskResultType(
                    method);

            var sectionType =
                GetEnumerableElementType(
                    taskResultType);

            var listType =
                typeof(List<>)
                    .MakeGenericType(
                        sectionType);

            var list =
                Activator.CreateInstance(
                    listType)
                ?? throw new InvalidOperationException(
                    "No fue posible crear la lista de secciones.");

            if (includeSection)
            {
                var section =
                    CreateSectionObject(
                        sectionType,
                        2,
                        "Linea de sal",
                        true);

                var addMethod =
                    listType.GetMethod(
                        "Add")
                    ?? throw new InvalidOperationException(
                        "No se encontró Add en la lista.");

                addMethod.Invoke(
                    list,
                    new[]
                    {
                        section
                    });
            }

            ConfigureTaskDefaultReturn(
                method.ReturnType,
                taskResultType,
                list);
        }

        /// <summary>
        /// Configura GetById sin depender del nombre exacto
        /// del DTO que retorna ISectionApplication.
        /// </summary>
        private void ConfigureGetByIdResult(
            bool exists,
            long sectionId = 2,
            string sectionName = "Linea de sal",
            bool isActive = true)
        {
            var method =
                typeof(ISectionApplication)
                    .GetMethod(
                        nameof(
                            ISectionApplication.GetById));

            if (method is null)
            {
                throw new InvalidOperationException(
                    "No se encontró ISectionApplication.GetById.");
            }

            var resultType =
                GetTaskResultType(
                    method);

            object? section =
                null;

            if (exists)
            {
                section =
                    CreateSectionObject(
                        resultType,
                        sectionId,
                        sectionName,
                        isActive);
            }

            ConfigureTaskDefaultReturn(
                method.ReturnType,
                resultType,
                section);
        }

        private static object CreateSectionObject(
            Type sectionType,
            long sectionId,
            string sectionName,
            bool isActive)
        {
            var section =
                Activator.CreateInstance(
                    sectionType)
                ?? throw new InvalidOperationException(
                    $"No fue posible crear {sectionType.Name}.");

            SetPropertyIfExists(
                section,
                "SectionId",
                sectionId);

            SetPropertyIfExists(
                section,
                "SectionName",
                sectionName);

            SetPropertyIfExists(
                section,
                "IsActive",
                isActive);

            return section;
        }

        private void ConfigureTaskDefaultReturn(
            Type taskType,
            Type taskResultType,
            object? result)
        {
            var fromResultMethod =
                typeof(Task)
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Static)
                    .Single(method =>
                        method.Name ==
                            nameof(Task.FromResult) &&
                        method.IsGenericMethodDefinition);

            var task =
                fromResultMethod
                    .MakeGenericMethod(
                        taskResultType)
                    .Invoke(
                        null,
                        new[]
                        {
                            result
                        });

            if (task is null)
            {
                throw new InvalidOperationException(
                    "No fue posible crear el Task.");
            }

            var setReturnsDefault =
                typeof(Mock<ISectionApplication>)
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Instance)
                    .Single(method =>
                        method.Name ==
                            "SetReturnsDefault" &&
                        method.IsGenericMethodDefinition);

            setReturnsDefault
                .MakeGenericMethod(
                    taskType)
                .Invoke(
                    _sectionApplicationMock,
                    new[]
                    {
                        task
                    });
        }

        private static Type GetTaskResultType(
            MethodInfo method)
        {
            if (!method.ReturnType.IsGenericType)
            {
                throw new InvalidOperationException(
                    $"{method.Name} debe retornar Task<T>.");
            }

            return method.ReturnType
                .GetGenericArguments()[0];
        }

        private static Type GetEnumerableElementType(
            Type enumerableType)
        {
            if (enumerableType.IsArray)
            {
                return enumerableType
                    .GetElementType()
                    ?? throw new InvalidOperationException(
                        "No se pudo determinar el tipo del arreglo.");
            }

            if (enumerableType.IsGenericType &&
                enumerableType.GetGenericTypeDefinition() ==
                typeof(IEnumerable<>))
            {
                return enumerableType
                    .GetGenericArguments()[0];
            }

            var enumerableInterface =
                enumerableType
                    .GetInterfaces()
                    .FirstOrDefault(type =>
                        type.IsGenericType &&
                        type.GetGenericTypeDefinition() ==
                        typeof(IEnumerable<>));

            if (enumerableInterface is null)
            {
                throw new InvalidOperationException(
                    $"No se pudo determinar el elemento de {enumerableType.Name}.");
            }

            return enumerableInterface
                .GetGenericArguments()[0];
        }

        private static void SetPropertyIfExists(
            object target,
            string propertyName,
            object value)
        {
            var property =
                target
                    .GetType()
                    .GetProperty(
                        propertyName);

            if (property is null)
            {
                throw new InvalidOperationException(
                    $"No se encontró la propiedad {propertyName} " +
                    $"en {target.GetType().Name}.");
            }

            property.SetValue(
                target,
                value);
        }
    }
}